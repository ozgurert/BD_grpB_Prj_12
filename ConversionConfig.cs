using System.Collections.Generic;

namespace RequirementsConverter
{
    // Defines a mapping from an input field/path to an output property
    public class FieldMapping
    {
        // For JSON: JSON Path (e.g., "data.uuid") or property name
        // For CSV: Column Header Name (if HasHeaderRow) or 0-based index (e.g., "0", "1")
        public string InputFieldName { get; set; }

        // Name of the property in the target C# model (e.g., "Id", "Title")
        public string OutputPropertyName { get; set; }

        // Optional: Data type for parsing/conversion (e.g., "string", "int", "bool", "datetime")
        public string DataType { get; set; }

        // Add other configuration options here (e.g., default values, required field, transformation rules)
    }

    // Configuration for JSON input
    public class JsonInputConfig
    {
        // JSON path to the array of items to process (e.g., "data.requirements.items")
        // If the root is the array, leave empty or specify "$"
        public string RootPath { get; set; }

        // List of mappings from JSON fields to model properties
        public List<FieldMapping> FieldMappings { get; set; }
    }

    // Configuration for CSV input
    public class CsvInputConfig
    {
        // Does the CSV file have a header row?
        public bool HasHeaderRow { get; set; }

        // The delimiter used in the CSV file (e.g., ",", ";", "\t")
        public string Delimiter { get; set; } = ",";

        // List of mappings from CSV columns (by header name or index) to model properties
        public List<FieldMapping> FieldMappings { get; set; }

        // Add other CSV-specific options (e.g., quote character, escape character)
    }

    // Overall conversion configuration
    public class ConversionConfig
    {
        // Type of the input file ("json" or "csv")
        public string InputType { get; set; }

        // Configuration specific to JSON input (only if InputType is "json")
        public JsonInputConfig JsonConfig { get; set; }

        // Configuration specific to CSV input (only if InputType is "csv")
        public CsvInputConfig CsvConfig { get; set; }

        // Type of the desired output format ("XML", "YAML", "CSV", "JSON", "OSLC_RM_RDF_XML")
        // OutputType is now handled by the ComboBox selection in Form1, but
        // could be part of the config if you have predefined conversion profiles.
        // public string OutputType { get; set; }

        // Add output-specific configurations here if needed
        // Example: XmlOutputConfig, CsvOutputConfig (to specify output column order/headers)
    }
}