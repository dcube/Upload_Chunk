namespace VideoChunk.Domain
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class Result<T> : Result
    {
        public T Value { get; set; }
    }
}
