namespace Cleriq.DTOs
{
    public class AuthDto
    {
        public record InregistrareDto(string Email, string Parola, string NumeComplet, string Rol);
        public record LoginDto(string Email, string Parola);
        public record RefreshDto(string RefreshToken);
    }
}