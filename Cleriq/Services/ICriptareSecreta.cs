namespace Cleriq.Services;

public interface ICriptareSecreta
{
    string Cripteaza(string textClar);
    string Decripteaza(string textCriptat);
}