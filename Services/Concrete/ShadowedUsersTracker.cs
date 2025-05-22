using System.Text;

namespace Server.Services.Concrete;

public class ShadowedUsersTracker
{
    private readonly List<(string Username, string Action)> Actions = new();

    public void AddAction(string username, string action)
    {
        lock (Actions)
        {
            Actions.Add((username, action));
        }
    }

    public string GetAllActions()
    {
        StringBuilder builder = new();
        List<string> actionsMerged;
        lock (Actions)
        {
            actionsMerged = Actions.Select(action => $"{action.Username}: {action.Action}\n").ToList();
        }
        
        foreach (var action in actionsMerged)
        {
            builder.Append(action);
        }
        return builder.ToString();
    }
}