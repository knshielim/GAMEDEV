using System;

public interface ISaveable
{
    // Unique key untuk object ini di SaveData
    string SaveId { get; }

    // Tulis data object -> SaveData
    void Save(SaveData data);

    // Baca data -> apply ke object
    void Load(SaveData data);
}
