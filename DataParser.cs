using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // For LINQ methods like Select, ToList
using System.Text.Json; // Using System.Text.Json for JSON parsing
using CsvHelper; // Using CsvHelper for CSV parsing
using CsvHelper.Configuration; // Required for CsvConfiguration
using System.Globalization; // Required for CultureInfo
using System.Reflection; // For using reflection to set properties

namespace RequirementsConverter
{
    public static class DataParser
    {
        // Parses JSON content based on the provided configuration
        public static List<Requirement> ParseJson(string jsonContent, JsonInputConfig config)
        {
            if (config == null || config.FieldMappings == null)
            {
                throw new ArgumentNullException(nameof(config), "JSON configuration and field mappings cannot be null.");
            }

            var requirements = new List<Requirement>();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(jsonContent))
                {
                    JsonElement root = document.RootElement;
                    JsonElement itemsElement;

                    // Navigate to the RootPath if specified
                    if (!string.IsNullOrEmpty(config.RootPath) && config.RootPath != "$")
                    {
                        // Simple path navigation (handle nested objects)
                        string[] pathSegments = config.RootPath.Split('.');
                        JsonElement currentElement = root;
                        bool pathFound = true;
                        foreach (var segment in pathSegments)
                        {
                            if (currentElement.ValueKind == JsonValueKind.Object && currentElement.TryGetProperty(segment, out JsonElement nextElement))
                            {
                                currentElement = nextElement;
                            }
                            else
                            {
                                pathFound = false;
                                break;
                            }
                        }
                        if (!pathFound || currentElement.ValueKind != JsonValueKind.Array)
                        {
                            throw new InvalidOperationException($"Could not find the array at the specified RootPath: {config.RootPath}");
                        }
                        itemsElement = currentElement;
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        // Root element is the array
                        itemsElement = root;
                    }
                    else
                    {
                        throw new InvalidOperationException("JSON root element is not an array and no valid RootPath to an array was provided.");
                    }


                    if (itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in itemsElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                var requirement = new Requirement();

                                // Use reflection to get the Requirement type properties once
                                var requirementType = typeof(Requirement);

                                foreach (var mapping in config.FieldMappings)
                                {
                                    // Find the property on the Requirement object
                                    var propertyInfo = requirementType.GetProperty(mapping.OutputPropertyName);
                                    if (propertyInfo == null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Warning: Property '{mapping.OutputPropertyName}' not found on Requirement class.");
                                        continue; // Skip mapping if property doesn't exist
                                    }

                                    // Try to get the JSON property based on the InputFieldName
                                    if (item.TryGetProperty(mapping.InputFieldName, out JsonElement propertyElement))
                                    {
                                        try
                                        {
                                            // Handle List<string> properties (expecting a JSON array of strings)
                                            if (propertyInfo.PropertyType == typeof(List<string>))
                                            {
                                                if (propertyElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    var stringList = new List<string>();
                                                    foreach (var element in propertyElement.EnumerateArray())
                                                    {
                                                        if (element.ValueKind == JsonValueKind.String)
                                                        {
                                                            stringList.Add(element.GetString());
                                                        }
                                                        // Optionally handle other value kinds in the array or log a warning
                                                    }
                                                    propertyInfo.SetValue(requirement, stringList);
                                                }
                                                else if (propertyElement.ValueKind == JsonValueKind.String)
                                                {
                                                    // Handle case where a List<string> property is represented as a single string in JSON (e.g., comma-separated)
                                                    // This requires specific logic based on the expected string format.
                                                    // For now, we'll just add the single string as the only item in the list.
                                                    var singleItemList = new List<string> { propertyElement.GetString() };
                                                    propertyInfo.SetValue(requirement, singleItemList);
                                                    System.Diagnostics.Debug.WriteLine($"Warning: Expected JSON array for '{mapping.InputFieldName}', but found a string. Treating as a single item list.");
                                                }
                                                else if (propertyElement.ValueKind != JsonValueKind.Null && propertyElement.ValueKind != JsonValueKind.Undefined)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"Warning: Expected JSON array or string for '{mapping.InputFieldName}' (mapped to List<string>), but found {propertyElement.ValueKind}. Skipping.");
                                                }
                                                // If ValueKind is Null or Undefined, the property remains null (default for List<string>)
                                            }
                                            // Handle other property types (string, int, bool, datetime etc.)
                                            else
                                            {
                                                object value = null;
                                                switch (mapping.DataType?.ToLower())
                                                {
                                                    case "string":
                                                        // GetString() handles Null by returning null
                                                        value = propertyElement.ValueKind == JsonValueKind.String ? propertyElement.GetString() : propertyElement.GetRawText(); // GetRawText can handle non-string types too
                                                        break;
                                                    case "int":
                                                        if (propertyElement.ValueKind == JsonValueKind.Number) value = propertyElement.GetInt32();
                                                        break;
                                                    case "bool":
                                                        if (propertyElement.ValueKind == JsonValueKind.True || propertyElement.ValueKind == JsonValueKind.False) value = propertyElement.GetBoolean();
                                                        break;
                                                    case "datetime":
                                                        if (propertyElement.ValueKind == JsonValueKind.String)
                                                        {
                                                            if (propertyElement.TryGetDateTime(out DateTime dt)) value = dt;
                                                            else if (propertyElement.TryGetDateTimeOffset(out DateTimeOffset dto)) value = dto.DateTime;
                                                            else if (DateTime.TryParse(propertyElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDt)) value = parsedDt;
                                                        }
                                                        break;
                                                    default: // Default to string if DataType is not specified or recognized
                                                             // GetString() handles Null by returning null
                                                        value = propertyElement.ValueKind == JsonValueKind.String ? propertyElement.GetString() : propertyElement.GetRawText();
                                                        break;
                                                }
                                                // Only set value if it's not null (unless the property type is nullable)
                                                // Or handle null explicitly if needed
                                                if (value != null || (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) || propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(List<string>))
                                                {
                                                    propertyInfo.SetValue(requirement, value);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error setting property '{mapping.OutputPropertyName}' from JSON field '{mapping.InputFieldName}': {ex.Message}");
                                            // Handle conversion errors (e.g., log, skip this mapping)
                                        }
                                    }
                                    else
                                    {
                                        // Handle missing input field (e.g., log a warning)
                                        System.Diagnostics.Debug.WriteLine($"Warning: JSON field '{mapping.InputFieldName}' not found in an item.");
                                        // Property will remain its default value (null for reference types, 0/false for value types)
                                    }
                                }
                                requirements.Add(requirement);
                            }
                        }
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                // Catch JSON parsing specific errors
                throw new InvalidOperationException("Error parsing JSON content.", jsonEx);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during JSON parsing
                throw new InvalidOperationException("An unexpected error occurred during JSON parsing.", ex);
            }

            return requirements;
        }

        // Parses CSV content based on the provided configuration
        public static List<Requirement> ParseCsv(string csvContent, CsvInputConfig config)
        {
            if (config == null || config.FieldMappings == null)
            {
                throw new ArgumentNullException(nameof(config), "CSV configuration and field mappings cannot be null.");
            }

            var requirements = new List<Requirement>();

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = config.HasHeaderRow,
                Delimiter = config.Delimiter,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim, // Trim whitespace from fields
                // Updated BadDataFound handler to use context.Context properties
                BadDataFound = context =>
                {
                    // Access raw record and field index/identifier via context.Context
                    var rawRecord = context.Context.Parser.RawRecord;
                    var fieldIdentifier = context.Context.Reader.CurrentIndex; // Get the index of the field

                    // If headers are present, try to get the header name
                    string fieldName = fieldIdentifier.ToString(); // Default to index as string
                    if (context.Context.Reader.HeaderRecord != null && fieldIdentifier < context.Context.Reader.HeaderRecord.Length)
                    {
                        fieldName = context.Context.Reader.HeaderRecord[fieldIdentifier];
                    }


                    System.Diagnostics.Debug.WriteLine($"Bad data found on raw record: '{rawRecord}', field index/name: '{fieldName}'.");
                    // You can log this, skip the row, or throw an exception
                }
            };

            try
            {
                using (var reader = new StringReader(csvContent))
                using (var csv = new CsvHelper.CsvReader(reader, csvConfig))
                {
                    // If there's a header, read it to make mappings by name work
                    if (config.HasHeaderRow)
                    {
                        csv.Read();
                        csv.ReadHeader(); // Assumes the first record is the header
                    }

                    // Get the Requirement type properties once
                    var requirementType = typeof(Requirement);

                    while (csv.Read())
                    {
                        var requirement = new Requirement();

                        foreach (var mapping in config.FieldMappings)
                        {
                            string inputValue = null;

                            // Find the property on the Requirement object
                            var propertyInfo = requirementType.GetProperty(mapping.OutputPropertyName);
                            if (propertyInfo == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Property '{mapping.OutputPropertyName}' not found on Requirement class.");
                                continue; // Skip mapping if property doesn't exist
                            }

                            try
                            {
                                if (config.HasHeaderRow)
                                {
                                    // Try get field by name (from header)
                                    // Use TryGetField to avoid exception on missing column
                                    if (csv.TryGetField<string>(mapping.InputFieldName, out string fieldStringValue))
                                    {
                                        inputValue = fieldStringValue;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Warning: CSV header '{mapping.InputFieldName}' not found in the header row.");
                                        // inputValue remains null, property will be default value
                                    }
                                }
                                else
                                {
                                    // Get field by index (if no header)
                                    if (int.TryParse(mapping.InputFieldName, out int columnIndex))
                                    {
                                        // Use TryGetField to avoid exception on index out of range
                                        if (csv.TryGetField<string>(columnIndex, out string fieldStringValue))
                                        {
                                            inputValue = fieldStringValue;
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Warning: CSV column index '{mapping.InputFieldName}' out of range for the current record.");
                                            // inputValue remains null
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException($"CSV configuration with no header requires numeric column indices in FieldMappings, but found '{mapping.InputFieldName}'.");
                                    }
                                }

                                // If we successfully got an input value (even if it's an empty string)
                                // and the property exists, attempt to set the value.
                                if (propertyInfo != null && inputValue != null)
                                {
                                    // Handle List<string> properties (expecting a comma-separated string in CSV)
                                    if (propertyInfo.PropertyType == typeof(List<string>))
                                    {
                                        // Split the string by comma, remove empty entries and trim whitespace
                                        var stringList = inputValue.Split(new[] { config.Delimiter }, StringSplitOptions.RemoveEmptyEntries)
                                                                   .Select(s => s.Trim())
                                                                   .Where(s => !string.IsNullOrWhiteSpace(s)) // Remove items that are just whitespace after trimming
                                                                   .ToList();
                                        propertyInfo.SetValue(requirement, stringList);
                                    }
                                    // Handle other property types (string, int, bool, datetime etc.)
                                    else
                                    {
                                        object value = inputValue; // Default to string

                                        switch (mapping.DataType?.ToLower())
                                        {
                                            case "int":
                                                if (int.TryParse(inputValue, out int intValue)) value = intValue;
                                                else System.Diagnostics.Debug.WriteLine($"Warning: Could not parse '{inputValue}' as int for property '{mapping.OutputPropertyName}'.");
                                                break;
                                            case "bool":
                                                if (bool.TryParse(inputValue, out bool boolValue)) value = boolValue;
                                                else System.Diagnostics.Debug.WriteLine($"Warning: Could not parse '{inputValue}' as bool for property '{mapping.OutputPropertyName}'.");
                                                break;
                                            case "datetime":
                                                if (DateTime.TryParse(inputValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtValue)) value = dtValue;
                                                else System.Diagnostics.Debug.WriteLine($"Warning: Could not parse '{inputValue}' as DateTime for property '{mapping.OutputPropertyName}'.");
                                                break;
                                            default: // Default to string
                                                value = inputValue; // Use the raw input string
                                                break;
                                        }

                                        // Set the value if it's not null or if the target type is string/nullable
                                        if (value != null || propertyInfo.PropertyType == typeof(string) || (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                                        {
                                            propertyInfo.SetValue(requirement, value);
                                        }
                                    }
                                }
                                // If inputValue is null (e.g., missing column), the property remains its default value
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error processing CSV field '{mapping.InputFieldName}' for property '{mapping.OutputPropertyName}': {ex.Message}");
                                // Handle mapping/conversion errors for this specific field
                            }
                        }
                        requirements.Add(requirement);
                    }
                }
            }
            catch (CsvHelperException csvEx)
            {
                // Catch CsvHelper specific errors
                throw new InvalidOperationException("Error parsing CSV content.", csvEx);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during CSV parsing
                throw new InvalidOperationException("An unexpected error occurred during CSV parsing.", ex);
            }


            return requirements;
        }
    }
}
