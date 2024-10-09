using System;

[Flags]
public enum CharacterState
{
    None = 0,
    Moving = 1 << 0,
    InGroup = 1 << 1,
    Idle = 1 << 2,
    Interacting = 1 << 3,
    Acclimating = 1 << 4,
    PerformingAction = 1 << 5,
    Chatting = 1 << 6,
    Collaborating = 1 << 7,
    Cooldown = 1 << 8,
    FormingGroup = 1 << 9
}