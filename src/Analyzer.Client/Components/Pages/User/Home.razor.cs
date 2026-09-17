namespace Analyzer.Client.Components.Pages.User;

using Microsoft.AspNetCore.Components;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using MudBlazor;
using Serilog;

using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;
using Analyzer.Client.Models;
using Analyzer.Client.Components.Dialogs;
using Analyzer.Client.Components.Widgets;
using Blazor.Diagrams.Core.Geometry;

using BlazorLinkModel = Blazor.Diagrams.Core.Models.LinkModel;
using LinkModel = Models.LinkModel;
using Blazor.Diagrams.Core.Anchors;

public partial class Home : ComponentBase, IDisposable
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private NavigationManager NavManager { get; set; } = default!;

    // Основное состояние страницы
    // ===========================
    private BlazorDiagram? Diagram { get; set; } = null;
    private bool _isLoadingGraph = true;
    // ===========================

    // Состояния панелей 
    // ===========================
    private bool _isComponentPropertiesPanelOpen = false;
    private ComponentModel? _selectedComponentModel;

    private bool _isLinkPropertiesPanelOpen = false;
    private LinkModel? _selectedLinkModel;
    private string? _selectedLinkSourceName;
    private string? _selectedLinkTargetName;
    // ===========================

    // Состояние симуляции
    // ===========================
    private bool _isAnalysisMode = false;
    // ===========================

    // Управление системами
    // ===========================
    private IReadOnlyCollection<ITSystemDto>? _systems;
    private Guid? _selectedSystemId;
    // ===========================

    // Пользователь, которому принадлежит текущая сессия
    // ===========================
    private UserDto _loggedUser = default!;
    // ===========================

    // =================================================
    // ИНИЦИАЛИЗАЦИЯ И ЖИЗНЕННЫЙ ЦИКЛ
    // =================================================
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var user = await Http.GetFromJsonAsync<UserDto>("api/v1/users/me");
            if (user is null)
            {
                Log.Error("Ошибка получения профиля пользователя.");
                NavManager.NavigateTo("/logout", forceLoad: true);
                return;
            }
            _loggedUser = user;

            _systems = await Http.GetFromJsonAsync<IReadOnlyCollection<ITSystemDto>>(
                $"api/v1/systems/?teamId={_loggedUser.TeamId}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки списка систем.");
            Snackbar.Add("Не удалось получить список систем", Severity.Error);
        }

        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = false,
            Zoom = { Enabled = true, Inverse = true },
            Links = { DefaultRouter = new NormalRouter(), DefaultPathGenerator = new SmoothPathGenerator() }
        };

        Diagram = new BlazorDiagram(options);
        Diagram.RegisterComponent<ComponentModel, ComponentWidget>();

        Diagram.PointerDoubleClick += OnComponentSelected;
        Diagram.PointerClick += OnCanvasOrLinkClicked;

        Diagram.Links.Added += OnLinkAdded;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_selectedSystemId is not null)
            {
                await RefreshGraphAsync();
                StateHasChanged();
            }
        }
    }

    private async Task OnSystemSelectedAsync(Guid? systemId)
    {
        _selectedSystemId = systemId;

        _isComponentPropertiesPanelOpen = false;
        _isLinkPropertiesPanelOpen = false;

        if (_selectedSystemId is not null)
        {
            await RefreshGraphAsync();
        }
        else
        {
            Diagram?.Nodes.Clear();
            Diagram?.Links.Clear();
        }
    }

    // =================================================
    // СОБЫТИЯ КЛИКОВ ПО ГРАФУ
    // =================================================
    private async void OnComponentSelected(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model is ComponentModel componentModel &&
            _selectedComponentModel == componentModel)
        {
            _isComponentPropertiesPanelOpen = true;
            _isLinkPropertiesPanelOpen = false;
            StateHasChanged();
        }
    }

    private void OnCanvasOrLinkClicked(Model? model, Blazor.Diagrams.Core.Events.PointerEventArgs e)
    {
        if (model is LinkModel linkModel)
        {
            if (linkModel.Source.Model is PortModel sourcePort &&
                linkModel.Target.Model is PortModel targetPort)
            {
                _selectedLinkModel = linkModel;

                _selectedLinkSourceName = sourcePort.GetParent<ComponentModel>().Name;
                _selectedLinkTargetName = targetPort.GetParent<ComponentModel>().Name;

                _isLinkPropertiesPanelOpen = true;
                _isComponentPropertiesPanelOpen = false;
                StateHasChanged();
            }
            else
            {
                Console.WriteLine("Каким-то образом выбрана некорректная связь..");
                return;
            }
        }
        else if (model is ComponentModel componentModel)
        {
            // Убираем выделение с прошлой модели
            _selectedComponentModel?.IsSelected = false;
            _selectedComponentModel?.Refresh();

            _selectedComponentModel = componentModel;
            componentModel.IsSelected = true;
            componentModel.Refresh();
        }
        else if (model is null)
        {
            _selectedComponentModel?.IsSelected = false;
            _selectedComponentModel?.Refresh();
            _selectedComponentModel = null;

            _isComponentPropertiesPanelOpen = false;
            _isLinkPropertiesPanelOpen = false;

            StateHasChanged();
        }
    }

    private void OnLinkAdded(BaseLinkModel baseLink)
    {
        baseLink.TargetAttached += OnLinkTargetAttached;
    }

    private async void OnLinkTargetAttached(BaseLinkModel baseLink)
    {
        if (baseLink is BlazorLinkModel linkModel)
        {
            baseLink.TargetAttached -= OnLinkTargetAttached;

            if (linkModel.Source.Model is not PortModel sourcePort
                || linkModel.Target.Model is not PortModel targetPort)
            {
                Snackbar.Add("Ошибка создания связи! Источник или приемник не является портом!", Severity.Error);
                Diagram?.Links.Remove(linkModel);
                return;
            }

            var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
            var targetId = targetPort.GetParent<ComponentModel>().ComponentId;

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<LinkConfigDialog>("Настройка связи", options);
            var result = await dialog.Result;

            if ((!result?.Canceled ?? false) && result!.Data is LinkConfigDialog.LinkConfigResult config)
            {
                var linkDto = new CreateLinkDto(sourceId, targetId, config.Severity, config.Protocol);
                var response = await Http.PostAsJsonAsync("api/v1/links/", linkDto);
                var createdLinkGuid = await response.Content.ReadFromJsonAsync<Guid>();

                Diagram?.Links.Remove(linkModel);

                var smartLink = new LinkModel(sourcePort, targetPort)
                {
                    LinkId = createdLinkGuid,
                    Severity = config.Severity,
                    Protocol = config.Protocol,
                    PathGenerator = new SmoothPathGenerator(),
                    Router = new NormalRouter()
                };

                UpdateDynamicPorts(smartLink);

                ApplyLinkStyles(smartLink, config.Severity, config.Protocol);
                Diagram?.Links.Add(smartLink);
            }
            else
            {
                Diagram?.Links.Remove(linkModel);
            }
        }
    }

    private async Task OpenAddComponentDialog()
    {
        if (_selectedSystemId is null)
            return;

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var parameters = new DialogParameters
        {
            ["SelectedSystemId"] = _selectedSystemId,
        };

        var dialog = await DialogService.ShowAsync<AddComponentDialog>("Новый компонент", parameters, options);

        var result = await dialog.Result;

        if (!result?.Canceled ?? false)
        {
            await RefreshGraphAsync();
        }
    }

    public async Task DeleteLink(LinkModel linkModel)
    {
        if (linkModel is null)
            return;
        Diagram?.Links.Remove(linkModel);
        _isLinkPropertiesPanelOpen = false;
        StateHasChanged();
    }

    // =================================================
    // ЗАГРУЗКА И ОТРИСОВКА ГРАФА
    // =================================================
    private async Task RefreshGraphAsync()
    {
        if (_selectedSystemId is null)
            return;

        _isLoadingGraph = true;
        Diagram?.Nodes.Clear();
        Diagram?.Links.Clear();

        try
        {
            var components = await Http.GetFromJsonAsync<List<ComponentDto>>($"api/v1/components/?systemId={_selectedSystemId}") ?? [];
            var links = await Http.GetFromJsonAsync<List<LinkDto>>($"api/v1/links/?systemId={_selectedSystemId}") ?? [];

            var compDict = RenderGraph(components);
            RenderLinks(links, compDict);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Ошибка во время выполнения запроса");
        }
        finally
        {
            _isLoadingGraph = false;
        }
    }

    public Dictionary<Guid, ComponentModel> RenderGraph(List<ComponentDto> components)
    {
        int x = 100, y = 100;
        var compDict = new Dictionary<Guid, ComponentModel>();
        foreach (var comp in components)
        {
            var node = new ComponentModel(comp.Id, comp.Name, comp.Type, new Point(x, y));
            Diagram?.Nodes.Add(node);
            compDict.Add(comp.Id, node);

            x += 250;
            if (x > 800) { x = 100; y += 200; }
        }

        return compDict;
    }

    private void RenderLinks(List<LinkDto> links, Dictionary<Guid, ComponentModel> compDict)
    {
        foreach (var link in links)
        {
            if (compDict.TryGetValue(link.SourceId, out var sourceNode) &&
                compDict.TryGetValue(link.TargetId, out var targetNode))
            {
                var sourcePort = sourceNode.GetPort(PortAlignment.Bottom)!;
                var targetPort = targetNode.GetPort(PortAlignment.Top)!;

                var linkModel = new LinkModel(sourcePort, targetPort)
                {
                    LinkId = link.Id,
                    Severity = link.Severity,
                    Protocol = link.Protocol,
                    PathGenerator = new SmoothPathGenerator(),
                    Router = new NormalRouter(),
                };

                ApplyLinkStyles(linkModel, link.Severity, link.Protocol);
                Diagram?.Links.Add(linkModel);
            }
        }
    }

    private async Task AutoArrangeNodesAsync()
    {
        if (Diagram is null || !Diagram.Nodes.Any())
            return;

        var nodes = Diagram.Nodes.OfType<ComponentModel>().ToList();
        var links = Diagram.Links.OfType<LinkModel>().ToList();

        if (nodes.Count < 2)
            return;

        double oldCenterX = nodes.Average(n => n.Position.X);
        double oldCenterY = nodes.Average(n => n.Position.Y);

        var degree = nodes.ToDictionary(n => n.ComponentId, n => 0);
        var edges = new List<(Guid Source, Guid Target)>();

        foreach (var link in links)
        {
            if (link.Source.Model is PortModel sourcePort && link.Target.Model is PortModel targetPort)
            {
                var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
                var targetId = targetPort.GetParent<ComponentModel>().ComponentId;
                edges.Add((sourceId, targetId));
                degree[sourceId]++;
                degree[targetId]++;
            }
        }

        var connectedNodes = nodes.Where(n => degree[n.ComponentId] > 0).ToList();
        var isolatedNodes = nodes.Where(n => degree[n.ComponentId] == 0).ToList();

        var pos = nodes.ToDictionary(n => n.ComponentId, n => new PointD(n.Position.X, n.Position.Y));

        if (connectedNodes.Any())
        {
            int iterations = 300;
            double aspectX = 3.0;
            double aspectY = 1.0;
            double k = 100.0;
            double t = 500.0;
            double tailForce = 5.0;

            var disp = connectedNodes.ToDictionary(n => n.ComponentId, n => new PointD(0, 0));

            for (int i = 0; i < iterations; i++)
            {
                foreach (var node in connectedNodes)
                    disp[node.ComponentId] = new PointD(0, 0);

                // Отталкивание
                for (int v = 0; v < connectedNodes.Count; v++)
                {
                    for (int u = 0; u < connectedNodes.Count; u++)
                    {
                        if (v == u) continue;

                        var idV = connectedNodes[v].ComponentId;
                        var idU = connectedNodes[u].ComponentId;

                        double dx = (pos[idV].X - pos[idU].X) / aspectX;
                        double dy = (pos[idV].Y - pos[idU].Y) / aspectY;
                        double distance = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.1);

                        double force = k * k / distance;
                        disp[idV].X += dx / distance * force * aspectX;
                        disp[idV].Y += dy / distance * force * aspectY;
                    }
                }

                // Притяжение
                foreach (var (source, target) in edges)
                {
                    double dx = (pos[source].X - pos[target].X) / aspectX;
                    double dy = (pos[source].Y - pos[target].Y) / aspectY;
                    double distance = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.1);

                    double force = distance * distance / k;

                    if (degree[source] == 1 || degree[target] == 1)
                        force *= tailForce;

                    double dxForce = dx / distance * force * aspectX;
                    double dyForce = dy / distance * force * aspectY;

                    disp[source].X -= dxForce;
                    disp[source].Y -= dyForce;
                    disp[target].X += dxForce;
                    disp[target].Y += dyForce;
                }

                // Гравитация (чтобы не разлетались отдельные куски графа)
                double cx = connectedNodes.Average(n => pos[n.ComponentId].X);
                double cy = connectedNodes.Average(n => pos[n.ComponentId].Y);
                foreach (var node in connectedNodes)
                {
                    var id = node.ComponentId;
                    disp[id].X += (cx - pos[id].X) * 0.05;
                    disp[id].Y += (cy - pos[id].Y) * 0.05;
                }

                // Применение
                foreach (var node in connectedNodes)
                {
                    var id = node.ComponentId;
                    double dx = disp[id].X;
                    double dy = disp[id].Y;
                    double distance = Math.Max(Math.Sqrt(dx * dx + dy * dy), 0.1);

                    pos[id].X += dx / distance * Math.Min(distance, t);
                    pos[id].Y += dy / distance * Math.Min(distance, t);
                }

                t *= 0.95;
            }

            // Поворот, если граф вертикальный
            double minX = connectedNodes.Min(n => pos[n.ComponentId].X);
            double maxX = connectedNodes.Max(n => pos[n.ComponentId].X);
            double minY = connectedNodes.Min(n => pos[n.ComponentId].Y);
            double maxY = connectedNodes.Max(n => pos[n.ComponentId].Y);

            if ((maxY - minY) > (maxX - minX))
            {
                foreach (var node in connectedNodes)
                {
                    var id = node.ComponentId;
                    double tempX = pos[id].X;
                    pos[id].X = pos[id].Y;
                    pos[id].Y = -tempX;
                }
            }
        }

        double scaleX = 1.2;
        double scaleY = 1.5;

        if (connectedNodes.Any())
        {
            double minX = connectedNodes.Min(n => pos[n.ComponentId].X);
            double minY = connectedNodes.Min(n => pos[n.ComponentId].Y);

            foreach (var node in connectedNodes)
            {
                pos[node.ComponentId].X = (pos[node.ComponentId].X - minX) * scaleX;
                pos[node.ComponentId].Y = (pos[node.ComponentId].Y - minY) * scaleY;
            }

            double newCenterX = connectedNodes.Average(n => pos[n.ComponentId].X);
            double newCenterY = connectedNodes.Average(n => pos[n.ComponentId].Y);

            double offsetX = oldCenterX - newCenterX;
            double offsetY = oldCenterY - newCenterY;

            foreach (var node in connectedNodes)
            {
                pos[node.ComponentId].X += offsetX;
                pos[node.ComponentId].Y += offsetY;
            }
        }

        if (isolatedNodes.Any())
        {
            double bottomY = connectedNodes.Any()
                ? connectedNodes.Max(n => pos[n.ComponentId].Y) + 200
                : oldCenterY;

            int cardWidth = 300;

            double startX = oldCenterX - (isolatedNodes.Count * cardWidth / 2.0);

            for (int i = 0; i < isolatedNodes.Count; i++)
            {
                var id = isolatedNodes[i].ComponentId;
                pos[id].X = startX + (i * cardWidth);
                pos[id].Y = bottomY;
            }
        }

        foreach (var node in nodes)
            node.SetPosition(pos[node.ComponentId].X, pos[node.ComponentId].Y);

        Diagram.Refresh();

        foreach (var link in Diagram.Links)
        {
            if (link is LinkModel linkModel)
                UpdateDynamicPorts(linkModel);
        }

        Snackbar.Add("Раскладка применена", Severity.Success);
    }

    // ===================================

    // Симуляция
    private async Task RunSimulationAsync(Guid initialFailedComponentId)
    {
        _isLoadingGraph = true;
        StateHasChanged();

        try
        {
            var result = await Http.GetFromJsonAsync<IReadOnlyCollection<Guid>>(
                $"api/v1/analysis/simulate/{initialFailedComponentId}");

            if (result is null || !result.Any())
            {
                Snackbar.Add("Сбой не привел к каскадному отказу других компонентов.", Severity.Success);
                return;
            }

            _isAnalysisMode = true;

            var allFailedIds = result.ToHashSet();
            allFailedIds.Add(initialFailedComponentId);

            foreach (var node in Diagram!.Nodes)
            {
                if (node is ComponentModel componentModel)
                {
                    if (allFailedIds.Contains(componentModel.ComponentId))
                    {
                        componentModel.IsFailed = true;
                        componentModel.IsDimmed = false;
                    }
                    else
                    {
                        componentModel.IsFailed = false;
                        componentModel.IsDimmed = true;
                    }
                    componentModel.Refresh();
                }
            }

            foreach (var link in Diagram.Links)
            {
                if (link is LinkModel linkModel)
                {
                    if (linkModel.Source.Model is PortModel sourcePort &&
                        linkModel.Target.Model is PortModel targetPort)
                    {
                        var sourceId = sourcePort.GetParent<ComponentModel>().ComponentId;
                        var targetId = targetPort.GetParent<ComponentModel>().ComponentId;

                        if (allFailedIds.Contains(sourceId) && allFailedIds.Contains(targetId))
                        {
                            linkModel.Color = "#ff3f5f";
                            linkModel.Width = 4;
                            linkModel.IsDimmed = false;
                        }
                        else
                        {
                            linkModel.Color = "#e2e8f0";
                            linkModel.IsDimmed = true;
                        }
                        linkModel.Refresh();
                    }
                }
            }

            Snackbar.Add($"Симуляция завершена. Затронуто узлов: {allFailedIds.Count}", Severity.Warning);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Ошибка при выполнении симуляции: {e.Message}", Severity.Error);
        }
        finally
        {
            _isLoadingGraph = false;
            StateHasChanged();
        }
    }

    private async Task RunCycleAnalysisAsync()
    {
        _isLoadingGraph = true;
        try
        {
            var result = await Http.GetFromJsonAsync<CycleAnalysisResultDto>(
                $"api/v1/analysis/cycles/{_selectedSystemId}");
            if (result is null || !result.Cycles.Any())
            {
                Snackbar.Add("Циклические зависимости не обнаружены.", Severity.Success);
                return;
            }

            _isAnalysisMode = true;
            var allNodesInCycles = result.Cycles.SelectMany(x => x).ToHashSet();

            foreach (var node in Diagram!.Nodes.OfType<ComponentModel>())
            {
                node.IsDimmed = !allNodesInCycles.Contains(node.ComponentId);
                node.Refresh();
            }

            foreach (var link in Diagram.Links.OfType<LinkModel>())
            {
                var sId = (link.Source.Model as PortModel)?.GetParent<ComponentModel>().ComponentId ?? Guid.Empty;
                var tId = (link.Target.Model as PortModel)?.GetParent<ComponentModel>().ComponentId ?? Guid.Empty;

                if (result.Cycles.Any(c => c.Contains(sId) && c.Contains(tId)))
                {
                    link.Color = "#9c27b0";
                    link.Width = 4;
                    link.IsDimmed = false;
                }
                else
                {
                    link.IsDimmed = true;
                }
                link.Refresh();
            }

            Snackbar.Add($"Найдено {result.Cycles.Count} циклов в системе", Severity.Warning);
        }
        catch (Exception) 
        { 
            Snackbar.Add("Ошибка анализа циклов", Severity.Error); 
        }
        finally 
        { 
            _isLoadingGraph = false; 
        }
    }

    private async Task RunSpofAnalysisAsync()
    {
        _isLoadingGraph = true;
        try
        {
            var result = await Http.GetFromJsonAsync<SpofAnalysisResultDto>(
                $"api/v1/analysis/spof/{_selectedSystemId}?threshold={Diagram?.Nodes.Count - 1 ?? 3}");
            if (result is null || !result.CriticalNodes.Any())
            {
                Snackbar.Add("Критичные единые точки отказа не найдены.", Severity.Success);
                return;
            }

            _isAnalysisMode = true;

            foreach (var node in Diagram!.Nodes.OfType<ComponentModel>())
            {
                if (result.CriticalNodes.ContainsKey(node.ComponentId))
                {
                    node.IsFailed = true;
                    node.IsDimmed = false;
                }
                else
                {
                    node.IsFailed = false;
                    node.IsDimmed = true;
                }
                node.Refresh();
            }

            foreach (var link in Diagram.Links.OfType<LinkModel>())
            {
                link.IsDimmed = true;
                link.Refresh();
            }

            Snackbar.Add($"Найдено {result.CriticalNodes.Count} узлов SPOF", Severity.Error);
        }
        catch (Exception) 
        { 
            Snackbar.Add("Ошибка анализа SPOF", Severity.Error); 
        }
        finally 
        { 
            _isLoadingGraph = false; 
        }
    }

    public async Task RunDecommissioningAsync(Guid targetId)
    {
        _isComponentPropertiesPanelOpen = false;
        _isLoadingGraph = true;
        try
        {
            var result = await Http.GetFromJsonAsync<DecommissioningResultDto>(
                $"api/v1/analysis/decommission/{targetId}");

            _isAnalysisMode = true;
            var impacted = result!.ImpactedComponentIds.ToHashSet();

            foreach (var node in Diagram!.Nodes.OfType<ComponentModel>())
            {
                if (node.ComponentId == targetId)
                {
                    node.IsDimmed = false;
                }
                else if (impacted.Contains(node.ComponentId))
                {
                    node.IsFailed = true;
                    node.IsDimmed = false;
                }
                else
                {
                    node.IsDimmed = true;
                    node.IsFailed = false;
                }
                node.Refresh();
            }

            Severity msgSeverity = impacted.Any() ? Severity.Error : Severity.Success;
            Snackbar.Add(result.Recommendation, msgSeverity);
        }
        catch (Exception) 
        { 
            Snackbar.Add("Ошибка анализа вывода", Severity.Error); 
        }
        finally 
        { 
            _isLoadingGraph = false; 
        }
    }

    public async Task RunDeploymentRiskAsync(Guid targetId)
    {
        _isComponentPropertiesPanelOpen = false;
        _isLoadingGraph = true;
        try
        {
            var result = await Http.GetFromJsonAsync<DeploymentRiskResultDto>(
                $"api/v1/analysis/deployment-risk/{targetId}");

            Severity severity = result!.RiskLevel switch
            {
                "Critical" => Severity.Error,
                "High" => Severity.Warning,
                "Medium" => Severity.Info,
                _ => Severity.Success
            };

            Snackbar.Add($"Риск: {result.RiskLevel}. Очки: {result.RiskScore}. {result.Summary}", severity);
        }
        catch (Exception) 
        { 
            Snackbar.Add("Ошибка оценки риска", Severity.Error); 
        }
        finally 
        { 
            _isLoadingGraph = false; 
        }
    }

    private void ResetAnalysisMode()
    {
        _isAnalysisMode = false;

        foreach (var node in Diagram!.Nodes)
        {
            if (node is ComponentModel componentModel)
            {
                componentModel.IsFailed = false;
                componentModel.IsDimmed = false;
                componentModel.Refresh();
            }
        }

        foreach (var link in Diagram.Links)
        {
            if (link is LinkModel linkModel)
            {
                linkModel.IsDimmed = false;
                ApplyLinkStyles(linkModel, linkModel.Severity, linkModel.Protocol);
                linkModel.Refresh();
            }
        }

        StateHasChanged();
    }
    // ===================================

    // =================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // =================================================
    private void ApplyLinkStyles(LinkModel linkModel,
        LinkSeverity severity, ProtocolType protocol)
    {
        linkModel.Color = severity switch
        {
            LinkSeverity.High => "#ff3f5f",
            LinkSeverity.Mid => "#ffb545",
            LinkSeverity.Low => "#3dcb6c",
            _ => "#74718e"
        };
        linkModel.Width = severity == LinkSeverity.High ? 3 : 2;
        linkModel.TargetMarker = LinkMarker.Arrow;

        linkModel.Labels.Clear();
        linkModel.Labels.Add(new LinkLabelModel(linkModel, protocol.ToString()));

        Diagram?.Refresh();
    }

    private void UpdateDynamicPorts(LinkModel link)
    {
        if (link.Source.Model is not PortModel sourcePort ||
            link.Target.Model is not PortModel targetPort)
            return;

        var source = sourcePort.GetParent<ComponentModel>();
        var target = targetPort.GetParent<ComponentModel>();

        var sourceCenterX = source.Position.X + 100;
        var sourceCenterY = source.Position.Y + 40;
        var targetCenterX = target.Position.X + 100;
        var targetCenterY = target.Position.Y + 40;

        var dx = targetCenterX - sourceCenterX;
        var dy = targetCenterY - sourceCenterY;

        PortAlignment sourceAlign, targetAlign;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            if (dx > 0)
            {
                sourceAlign = PortAlignment.Right;
                targetAlign = PortAlignment.Left;
            }
            else
            {
                sourceAlign = PortAlignment.Left;
                targetAlign = PortAlignment.Right;
            }
        }
        else
        {
            if (dy > 0)
            {
                sourceAlign = PortAlignment.Bottom;
                targetAlign = PortAlignment.Top;
            }
            else
            {
                sourceAlign = PortAlignment.Top;
                targetAlign = PortAlignment.Bottom;
            }
        }

        var newSourcePort = source.GetPort(sourceAlign);
        var newTargetPort = target.GetPort(targetAlign);

        if (newSourcePort is not null && newTargetPort is not null)
        {
            link.SetSource(new SinglePortAnchor(newSourcePort));
            link.SetTarget(new SinglePortAnchor(newTargetPort));

            link.Refresh();
        }
    }
    // ===================================

    public void Dispose()
    {
        Diagram?.PointerDoubleClick -= OnComponentSelected;
        Diagram?.PointerClick -= OnCanvasOrLinkClicked;
        Diagram?.Links.Added -= OnLinkAdded;
    }

    private class PointD(double x, double y)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
    }
}