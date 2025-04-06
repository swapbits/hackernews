namespace Stories.Application.Services;

public interface ICache 
{
    T? Get<T>(int key);
    void Put<T>(int key, T value);
}