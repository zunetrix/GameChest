using Dalamud.Game.Text;

namespace GameChest;

public interface IChatConsumer {
    void ProcessChatMessage(string senderFullName, string message, XivChatType chatType);
}
