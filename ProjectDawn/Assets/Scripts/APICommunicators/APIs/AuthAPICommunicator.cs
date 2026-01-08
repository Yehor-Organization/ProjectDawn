using System.Threading.Tasks;

public class AuthAPICommunicator : APIClientBase
{
    protected override bool RequiresAuthService => false;

    // -----------------------
    // LOGIN
    // -----------------------
    public Task<AuthTokens> Login(string username, string password)
    {
        return Post<AuthTokens>(
            "/Players/Login",
            new PlayerDTO
            {
                Username = username,
                Password = password
            },
            requiresAuth: false
        );
    }

    // -----------------------
    // REFRESH TOKEN
    // -----------------------
    public Task<AuthTokens> Refresh(string refreshToken)
    {
        return Post<AuthTokens>(
            "/Players/Refresh",
            new RefreshTokenDTO
            {
                RefreshToken = refreshToken
            },
            requiresAuth: false
        );
    }

    // -----------------------
    // REGISTER
    // -----------------------
    public Task<AuthTokens> Register(string username, string password)
    {
        return Post<AuthTokens>(
            "/Players/Register",
            new PlayerDTO
            {
                Username = username,
                Password = password
            },
            requiresAuth: false
        );
    }
}