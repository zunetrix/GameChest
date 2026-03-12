// Replaces Util/ChatBox.cs in the GameChest2 (test) compilation.
// Chat.SendMessage is a no-op so game logic can run without an FFXIV client.
namespace GameChest;

public static class Chat {
    public static void SendMessage(string message) { /* no-op in test compilation */ }
}
