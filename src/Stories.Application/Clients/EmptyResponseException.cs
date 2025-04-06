namespace Stories.Application.Clients;

public class EmptyResponseException : Exception
{
    public EmptyResponseException(string msg) : base(msg) 
    {
    }
}