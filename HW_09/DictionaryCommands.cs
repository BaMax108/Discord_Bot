using System.Collections.Generic;

namespace DiscordBot
{
    class DictionaryCommands
    {
        public Dictionary<string, string> CommandsDB { get; }

        public DictionaryCommands()
        {
            this.CommandsDB = new Dictionary<string, string>
            {
                
                // Справка
                {"start",       "!help"},
                {"!help",       "!help"},
                {"help",        "!help"},
                {"info",        "!help"},
                {"хелп",        "!help"},
                {"справка",     "!help"},
                // Приветствие
                {"!привет",     "!привет"},
                {"привет",      "!привет"},
                {"прив",        "!привет"},
                {"здарова",     "!привет"},
                {"здрасте",     "!привет"},
                {"здравс",      "!привет"},
                {"hello",       "!привет"},
                {"hi",          "!привет"},
                // Команды с точным соответствмем Ключ==Значение
                {"!cities",     "!cities"},
                {"!save",       "!save"},
                {"!myfiles",    "!myfiles"},
                {"!giveme",     "!giveme"},
            };
        }
    }
}