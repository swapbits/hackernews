namespace Stories.Application.Clients;

public class InvalidStatusException : Exception
{
    public InvalidStatusException(string msg) : base(msg) 
    {
    }
}