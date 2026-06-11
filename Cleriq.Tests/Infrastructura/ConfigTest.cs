namespace Cleriq.Tests.Infrastructura;

// Sursă unică de adevăr pentru configurarea de test.
// ATENȚIE: ConnectionStringDb arată spre CleriqTest — baza e ȘTEARSĂ și recreată la fiecare rulare.
public static class ConfigTest
{
    public const string ConnectionStringDb =
        "Server=.;Database=CleriqTest;Trusted_Connection=True;TrustServerCertificate=True";

    // Index 15 = dedicat testelor. Dev-ul folosește index 0 (default) — separare completă a cheilor.
    public const int RedisDatabaseIndex = 15;
    public const string ConnectionStringRedis = "localhost:6379,defaultDatabase=15";
    public const string ConnectionStringRedisAdmin = "localhost:6379,allowAdmin=true";

    public const string JwtKey =
        "cheie-jwt-exclusiv-pentru-teste-nu-se-foloseste-in-alt-mediu-0123456789";

    public const string SuperAdminEmail = "superadmin.test@cleriq.ro";
    public const string SuperAdminParola = "SuperAdminTest1!";
}