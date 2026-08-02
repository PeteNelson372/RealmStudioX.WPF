namespace RealmStudioX.WPF.Editor.Services
{
    public class MapObjectDescriptionService
    {
        private string _objectDescription = string.Empty;

        public string ObjectDescription
        {
            get => _objectDescription;
            private set => _objectDescription = value;
        }

        public MapObjectDescriptionService()
        {
        }

        public void ClearDescription()
        {
            _objectDescription = string.Empty;
        }

        public async Task GetMapObjectDescription(string query)
        {
            try
            {
                string? token = await AiIntegrationService.GetJwtTokenAsync();

                if (token != null)
                {
                    string? aiCallData = await AiIntegrationService.GetAiCallData(token);

                    if (aiCallData != null)
                    {
                        string? description = await AiIntegrationService.GetAIDescriptionAsync(aiCallData, query);

                        if (!string.IsNullOrEmpty(description))
                        {
                            _objectDescription = description;
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to retrieve AI call data.");
                    }
                }
                else
                {
                    throw new Exception("Failed to retrieve token for AI integration.");
                }
            }
            catch
            {
                throw;
            }
        }

        public string BuildAiQuery(string mapObjectTypeName, string? mapObjectName, string? mapObjectDetailType, List<string>? mapObjectCharacteristics)
        {
            string query = string.Empty;


            switch (mapObjectTypeName)
            {
                case "RealmStudioProject":
                    {
                        query = $"Generate a description for the fantasy map realm";
                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "RealmStudioMap":
                    {
                        query = $"Generate a description for the fantasy map";
                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "Landform":
                    {
                        query = $"Generate a description for the fantasy map landform";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "WaterSystem":
                    {
                        query = $"Generate a description for the fantasy map water system (watershed, drainage system, freshwater system, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "Lake":
                    {
                        query = $"Generate a description for the fantasy map lake (lake, pond, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "River":
                    {
                        query = $"Generate a description for the fantasy map river";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "WaterBody":
                    {
                        query = $"Generate a description for the fantasy map water body (lake, pond, river, stream, canal, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "MapPath":
                    {
                        query = $"Generate a description for the fantasy map path (road, trail, avenue, track, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "MapRegion":
                    {
                        query = $"Generate a description for the fantasy map region (country, county, state, area, ocean, bay, gulf, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                case "MapSymbol":
                    {
                        query = $"Generate a description for the fantasy map feature (tree, house, castle, hill, mountain, etc.)";

                        if (!string.IsNullOrEmpty(mapObjectDetailType))
                        {
                            query += $" of type '{mapObjectDetailType}'";
                        }
                    }
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(query))
            {
                if (!string.IsNullOrEmpty(mapObjectName))
                {
                    query += $" named '{mapObjectName}'";
                }

                if (mapObjectCharacteristics != null && mapObjectCharacteristics.Count > 0)
                {
                    query += $" with the following characteristics: {string.Join(", ", mapObjectCharacteristics)}";
                }

                // instruct the LLM not to use any markup in the generated description
                query += " Return plain text only. Do not use Markdown. Do not surround words with **, *, _, or backticks. Do not use headings or lists.";
            }

            return query;
        }
    }
}
