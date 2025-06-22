using CarInsuranceSalesBot.Models;

namespace CarInsuranceSalesBot.Services;

public class UserSessionManager
{
    private readonly Dictionary<long, UserSession> _sessions = new();

    public UserSession GetOrCreateUserSession(long userId)
    {
        if (_sessions.TryGetValue(key: userId, value: out UserSession? session))
        {
            return session;
        }

        session = new UserSession(userId) { Step = 0 };
        _sessions[userId] = session;

        return session;
    }
}
