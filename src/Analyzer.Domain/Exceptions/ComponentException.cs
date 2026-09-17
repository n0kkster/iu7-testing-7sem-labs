namespace Analyzer.Domain.Exceptions;

public class ComponentException(string messsage) : Exception(messsage)
{ }

public class InvalidComponentPropertyException(string message) : ComponentException(message)
{ }