using Newtonsoft.Json;

namespace DMsgBot.Config
{
    public class TelegramConfig
    {
        [JsonProperty("api_id")]
        public string api_id { get; private set; }

        [JsonProperty("api_hash")]
        public string api_hash { get; private set; }

        [JsonProperty("phone_number")]
        public string phone_number { get; private set; }

        [JsonProperty("verification_code")]
        public string verification_code { get; private set; }

        [JsonProperty("first_name")]
        public string first_name { get; private set; }

        [JsonProperty("last_name")]
        public string last_name { get; private set; }

        [JsonProperty("password")]
        public string password { get; private set; }

        [JsonIgnore]
        private string ConfigFile => Path.Combine(Environment.CurrentDirectory, "telegram.json");

        public TelegramConfig()
        {
            this.LoadConfig();
        }

        public TelegramConfig(string api_id, string api_hash, string phone_number)
        {
            this.api_id = api_id;
            this.api_hash = api_hash;
            this.phone_number = phone_number;
            this.Save();
        }

        public string Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(this.ConfigFile, json);

            return this.ConfigFile;
        }

        public bool LoadConfig()
        {
            if(!File.Exists(this.ConfigFile))
                return false;
            
            string json = File.ReadAllText(this.ConfigFile);

            JsonConvert.PopulateObject(json, this);

            return true;
        }

        public string Config(string cfg)
        {
            try
            {
                if (GetType().GetProperty(cfg) != null)
                    return GetType().GetProperty(cfg).GetValue(this).ToString();
            }
            catch
            {
            }

            return null;
        }

        public void SetVerificationCode(string code)
        {
            this.verification_code = code;
            this.Save();
        }

        public void SetFirstName(string first_name)
        {
            this.first_name = first_name;
            this.Save();
        }

        public void SetLastName(string last_name)
        {
            this.last_name = last_name;
            this.Save();
        }
    }
}
