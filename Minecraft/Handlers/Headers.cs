using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC_Server_Test.Minecraft
{
    public enum InHeader
    {
        //Client to server

        // State: Handshake
        Handshake = 0x00,

        // State: Status
        ServerListPing = 0x00,
        Ping = 0x01,

        // State: Login
        LoginRequest = 0x00,
        EncryptionResponse = 0x01,

        // State: Play
        KeepAlive = 0x00,
        ChatMessage = 0x01,
        UseEntity = 0x02,
        Player = 0x03,
        PlayerPosition = 0x04,
        PlayerLook = 0x05,
        PlayerPositionAndLook = 0x06,
        PlayerDigging = 0x07,
        PlayerBlockPlacement = 0x08,
        HeldItemChange = 0x09,
        Animation = 0x0a,
        EntityAction = 0x0b,
        CloseWindow = 0x0d,
        ClickWindow = 0x0e,
        ConfirmTransaction = 0x0f,
        CreativeInventoryAction = 0x10,
        EnchantItem = 0x11,
        UpdateSign = 0x12,
        PlayerAbilities = 0x13,
        TabComplete = 0x14,
        ClientSettings = 0x15,
        ClientStatus = 0x16,
        PluginMessage = 0x17,

        Inventory = 0x99,
        BlockChange = 0x99,
    }

    public enum OutHeader
    {
        //Server to client

        // State: Login  
        EncryptionRequest = 0x01,
        LoginSuccess = 0x02,
        SetCompression = 0x03,

        // State: play
        KeepAlive = 0x00,
        JoinGame = 0x01,
        ChatMessage = 0x02,
        TimeUpdate = 0x03,
        EntityEquipment = 0x04,
        SpawnPosition = 0x05,
        UpdateHealth = 0x06,
        Respawn = 0x07,
        PlayerPositionAndLook = 0x08,
        HeldItemChange = 0x09,
        UseBed = 0x0a,
        Animation = 0x0b,
        SpawnPlayer = 0x0c,
        CollectItem = 0x0d,
        SpawnObject = 0x0e,
        SpawnMob = 0x0f,
        SpawnPainting = 0x10,
        SpawnExperienceOrb = 0x11,
        EntityVelocity = 0x12,
        DestroyEntities = 0x13,
        Entity = 0x14,
        EntityRelativeMove = 0x15,
        EntityLook = 0x16,
        EntityLookRelativeMove = 0x17,
        EntityTeleport = 0x18,
        EntityHeadLook = 0x19,
        EntityStatus = 0x1a,
        AttachEntity = 0x1b,
        EntityMetadata = 0x1c,
        EntityEffect = 0x1d,
        RemoveEntityEffect = 0x1e,
        SetExperience = 0x1f,
        EntityProperties = 0x20,
        MapChunk = 0x21,
        MultiBlockChange = 0x22,
        BlockChange = 0x23,
        BlockAction = 0x24,
        BlockBreakAnimation = 0x25,
        MapChunkBulk = 0x26,
        Explosion = 0x27,
        Effect = 0x28,
        SoundEffect = 0x29,
        Particle = 0x2a,
        ChangeGameState = 0x2b,
        SpawnGlobalEntity = 0x2c,
        OpenWindow = 0x2d,
        CloseWindow = 0x2e,
        SetSlot = 0x2f,
        WindowItems = 0x30,
        WindowProperty = 0x31,
        ConfirmTransaction = 0x32,
        UpdateSign = 0x33,
        Map = 0x34,
        UpdateBlockEntity = 0x35,
        OpenSignEditor = 0x36,
        Statistics = 0x37,
        PlayerListItem = 0x38,
        PlayerAbilities = 0x39,
        TabComplete = 0x3a,

        ScoreboardObjective = 0x3b,
        UpdateScore = 0x3c,
        DisplayScoreboard = 0x3d,
        Teams = 0x3e,

        PluginMessage = 0x3f,
        Disconnect = 0x40,
        ServerDifficulty = 0x41,
        CombatEvent = 0x42,
        Camera = 0x43,
        WorldBorder = 0x44,
        Title = 0x45,
        PlayerListHeaderFooter = 0x47,
        ResourcePackSend = 0x48,
        UpdateEntityNBT = 0x49
    }
}
