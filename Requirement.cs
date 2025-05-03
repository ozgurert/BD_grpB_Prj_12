using System;
using System.Collections.Generic; // List<T> kullanmak için

namespace RequirementsConverter
{
    // Represents a requirement, serving as an intermediate data structure
    public class Requirement
    {
        public string Id { get; set; } // Corresponds to oslc_rm:identifier or dcterms:identifier
        public string Title { get; set; } // Corresponds to dcterms:title
        public string Description { get; set; } // Corresponds to dcterms:description

        public List<string> Stakeholders { get; set; } // CSV/JSON'daki 'stakeholders' sütunu için
        public List<string> Products { get; set; }     // CSV/JSON'daki 'products' sütunu için
        public List<string> Tags { get; set; }         // CSV/JSON'daki 'tags' sütunu için
        public List<string> WorkPackages { get; set; } // CSV/JSON'daki 'workPackages' sütunu için

        public DateTime? CreatedOn { get; set; } // dcterms:created için (Nullable DateTime)

    }
}