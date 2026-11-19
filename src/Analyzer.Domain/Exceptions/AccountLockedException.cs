namespace Analyzer.Domain.Exceptions;

public class AccountLockedException(string message) : Exception(message);