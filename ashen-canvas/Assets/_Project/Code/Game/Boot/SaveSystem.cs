using System.IO;
using AshenCanvas.Sim.Run;
using UnityEngine;

namespace AshenCanvas.Game.Boot
{
    /// <summary>Одно сохранение в JSON в папке данных приложения.</summary>
    public static class SaveSystem
    {
        static string PathToSave => Path.Combine(Application.persistentDataPath, "ashen_canvas_save.json");

        public static bool HasSave => File.Exists(PathToSave);

        public static void Save(GameState gs)
        {
            try { File.WriteAllText(PathToSave, JsonUtility.ToJson(gs, true)); }
            catch (IOException e) { Debug.LogWarning("Не удалось сохранить: " + e.Message); }
        }

        public static GameState Load()
        {
            if (!HasSave) return null;
            try
            {
                var gs = JsonUtility.FromJson<GameState>(File.ReadAllText(PathToSave));
                if (gs == null || gs.version != GameState.CurrentVersion) return null;
                return gs;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Сохранение повреждено: " + e.Message);
                return null;
            }
        }
    }
}
