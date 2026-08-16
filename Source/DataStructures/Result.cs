namespace DataStructures;

public abstract record Result<T> {}
public sealed record Success<T>(T Value) : Result<T>;
public sealed record Failure<T>(string Error) : Result<T>;