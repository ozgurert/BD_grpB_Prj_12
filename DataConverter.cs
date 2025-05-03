using System;
using System.Collections.Generic;
using System.IO; // System.IO.StringWriter için
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Text.Json; // JSON çıktısı için
using CsvHelper; // CSV çıktısı için
using YamlDotNet.Serialization; // YAML çıktısı için
using System.Globalization; // CultureInfo için

// dotNetRDF kütüphanesini kullanmak için gerekli using bildirimleri
// dotNetRDF'i NuGet ile yüklediyseniz bu usingleri ekleyin.
using VDS.RDF;         // IGraph, INode, Graph, NamespaceMap
using VDS.RDF.Writing;  // RdfXmlWriter
using VDS.RDF.Parsing;  // XmlSpecsHelper (datetime gibi XML Schema tipleri için)
// using VDS.RDF.Query; // Eğer SPARQL sorgulama yapacaksanız bu da gerekebilir.


namespace RequirementsConverter
{
    public static class DataConverter
    {
        // Converts a list of Requirements to XML
        public static string ConvertRequirementsToXml(List<Requirement> requirements)
        {
            XElement rootElement = new XElement("requirements");

            foreach (var req in requirements)
            {
                XElement reqElement = new XElement("requirement",
                    new XAttribute("id", req.Id ?? Guid.NewGuid().ToString()), // Id yoksa yeni bir GUID kullan
                    new XElement("title", req.Title ?? "Untitled"), // Başlık yoksa "Untitled" kullan
                    new XElement("description", req.Description ?? "") // Açıklama yoksa boş string kullan
                );

                // Stakeholders listesini XML'e ekleme
                if (req.Stakeholders != null && req.Stakeholders.Count > 0)
                {
                    // Her bir stakeholder için ayrı bir element oluştur
                    XElement stakeholdersElement = new XElement("stakeholders");
                    foreach (var stakeholder in req.Stakeholders)
                    {
                        if (!string.IsNullOrWhiteSpace(stakeholder))
                        {
                            stakeholdersElement.Add(new XElement("stakeholder", stakeholder.Trim()));
                        }
                    }
                    if (stakeholdersElement.HasElements) // Eğer hiç geçerli stakeholder varsa ekle
                    {
                        reqElement.Add(stakeholdersElement);
                    }
                }

                // Products listesini XML'e ekleme
                if (req.Products != null && req.Products.Count > 0)
                {
                    XElement productsElement = new XElement("products");
                    foreach (var product in req.Products)
                    {
                        if (!string.IsNullOrWhiteSpace(product))
                        {
                            productsElement.Add(new XElement("product", product.Trim()));
                        }
                    }
                    if (productsElement.HasElements)
                    {
                        reqElement.Add(productsElement);
                    }
                }

                // Tags listesini XML'e ekleme
                if (req.Tags != null && req.Tags.Count > 0)
                {
                    XElement tagsElement = new XElement("tags");
                    foreach (var tag in req.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            tagsElement.Add(new XElement("tag", tag.Trim()));
                        }
                    }
                    if (tagsElement.HasElements)
                    {
                        reqElement.Add(tagsElement);
                    }
                }

                // WorkPackages listesini XML'e ekleme
                if (req.WorkPackages != null && req.WorkPackages.Count > 0)
                {
                    XElement workPackagesElement = new XElement("workPackages");
                    foreach (var workPackage in req.WorkPackages)
                    {
                        if (!string.IsNullOrWhiteSpace(workPackage))
                        {
                            workPackagesElement.Add(new XElement("workPackage", workPackage.Trim()));
                        }
                    }
                    if (workPackagesElement.HasElements)
                    {
                        reqElement.Add(workPackagesElement);
                    }
                }

                // dcterms:created alanını XML'e ekleme (varsa)
                if (req.CreatedOn.HasValue)
                {
                    // XML'de ISO 8601 formatı yaygın kullanılır, XSD datetime da buna benzer.
                    reqElement.Add(new XElement("createdOn", req.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
                }


                rootElement.Add(reqElement);
            }

            var settings = new XmlWriterSettings
            {
                Indent = true, // Okunurluk için girinti ekle
                OmitXmlDeclaration = true, // XML deklarasyonunu atla (isteğe bağlı)
                Encoding = Encoding.UTF8 // UTF-8 encoding kullan
            };

            // System.IO.StringWriter kullanarak tam nitelikli adı belirtme
            using (var stringWriter = new System.IO.StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                rootElement.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
        }

        // Converts a list of Requirements to YAML
        public static string ConvertRequirementsToYaml(List<Requirement> requirements)
        {
            // YamlDotNet Serializer kullanarak List<Requirement>'ı YAML'ye dönüştür
            // List<string> özellikleri YamlDotNet tarafından doğal olarak YAML listeleri olarak serileştirilmelidir.
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(requirements);
            return yaml;
        }

        // Converts a list of Requirements to CSV
        // Bu implementasyon List<string> alanlarını virgülle ayrılmış tek string olarak yazar.
        public static string ConvertRequirementsToCsv(List<Requirement> requirements)
        {
            // System.IO.StringWriter kullanarak tam nitelikli adı belirtme
            using (var writer = new System.IO.StringWriter())
            // CsvHelper.CsvWriter kullanarak tam nitelikli adı belirtme
            using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Manuel olarak başlıkları yaz (Requirement sınıfındaki tüm public property'leri alabiliriz)
                // Ya da sadece istediğimiz başlıkları listeleriz.
                // Basitlik için manuel olarak listeliyoruz ve yeni alanları ekliyoruz.
                csv.WriteField("Id");
                csv.WriteField("Title");
                csv.WriteField("Description");
                csv.WriteField("Stakeholders"); // Yeni başlık
                csv.WriteField("Products");     // Yeni başlık
                csv.WriteField("Tags");         // Yeni başlık
                csv.WriteField("WorkPackages"); // Yeni başlık
                csv.WriteField("CreatedOn");    // Yeni başlık
                // Requirement sınıfına eklediğiniz başka alanlar varsa buraya başlıklarını ekleyin.
                csv.NextRecord(); // Başlık satırını bitir

                // Her gereksinimin alanlarını yaz
                foreach (var req in requirements)
                {
                    csv.WriteField(req.Id);
                    csv.WriteField(req.Title);
                    csv.WriteField(req.Description);
                    // List<string> alanlarını virgülle ayırarak tek string olarak yaz
                    // Eğer liste null veya boşsa boş string yaz
                    csv.WriteField(req.Stakeholders != null ? string.Join(", ", req.Stakeholders) : "");
                    csv.WriteField(req.Products != null ? string.Join(", ", req.Products) : "");
                    csv.WriteField(req.Tags != null ? string.Join(", ", req.Tags) : "");
                    csv.WriteField(req.WorkPackages != null ? string.Join(", ", req.WorkPackages) : "");
                    // CreatedOn alanını yaz (varsa)
                    csv.WriteField(req.CreatedOn.HasValue ? req.CreatedOn.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) : ""); // CSV için XSD formatı

                    // Requirement sınıfına eklediğiniz başka alanlar varsa buraya değerlerini yazın.
                    csv.NextRecord(); // Veri satırını bitir
                }

                return writer.ToString();
            }
        }

        // Converts a list of Requirements to JSON
        public static string ConvertRequirementsToJson(List<Requirement> requirements)
        {
            // System.Text.Json kullanarak List<Requirement>'ı JSON stringine dönüştür
            // List<string> özellikleri System.Text.Json tarafından doğal olarak JSON dizileri olarak serileştirilmelidir.
            var options = new JsonSerializerOptions { WriteIndented = true }; // Okunurluk için girintili yazdır
            return System.Text.Json.JsonSerializer.Serialize(requirements, options);
        }

        // Converts a list of Requirements to OSLC RM 2.0 RDF/XML
        // THIS IS WHERE YOU IMPLEMENT THE ACTUAL RDF/XML GENERATION
        public static string ConvertRequirementsToOslcRmXml(List<Requirement> requirements)
        {
            // Bir RDF grafı oluşturun
            IGraph graph = new Graph();

            // Gerekli Namespace'leri tanımlayın (OSLC RM, Dublin Core, RDF vb.)
            Uri baseUri = new Uri("http://your.base.uri/"); // Burayı kendi proje veya sistem URI'nizle değiştirin
            // NamespaceMap'e URI'leri string olarak ekleme
            graph.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            graph.NamespaceMap.AddNamespace("dcterms", new Uri("http://purl.org/dc/terms/")); // Dublin Core Terms
            graph.NamespaceMap.AddNamespace("oslc", new Uri("http://open-services.net/ns/core#")); // OSLC Core Namespace
            graph.NamespaceMap.AddNamespace("oslc_rm", new Uri("http://open-services.net/ns/rm#")); // OSLC RM Namespace
            graph.NamespaceMap.AddNamespace("xsd", new Uri(NamespaceMapper.XMLSCHEMA)); // XSD Datatype Namespace
            // İhtiyacınız olursa başka namespace'ler de ekleyebilirsiniz (örn: foaf, skos, custom).
            graph.NamespaceMap.AddNamespace("ex", baseUri); // Örnek olarak kendi temel URI'niz için bir prefix


            // Her bir Requirement nesnesini RDF grafına ekleyin
            foreach (var req in requirements)
            {
                // Gereksinim için bir URI düğümü oluşturun
                // URI'nin benzersiz ve resolvable (çözülebilir) olması önemlidir.
                // Genellikle temel URI + kaynak tipi + kimlik şeklinde oluşturulur.
                // Id boş veya null ise, geçici bir URI oluşturabilir veya bu gereksinimi atlayabilirsiniz.
                if (string.IsNullOrEmpty(req.Id))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Requirement with title '{req.Title}' has no ID. Skipping RDF generation for this item.");
                    continue; // ID'si olmayan gereksinimleri atla
                }
                Uri requirementUri = new Uri(baseUri, $"requirements/{req.Id}");
                INode requirementNode = graph.CreateUriNode(requirementUri);

                // RDF tipini ekleyin (Bu bir OSLC RM Gereksinimidir)
                // rdf:type URI'sini NamespaceMap üzerinden alma
                Uri rdfTypeUri = graph.NamespaceMap.GetNamespaceUri("rdf").ExtendLocalName("type"); // "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"
                Uri requirementTypeUri = graph.NamespaceMap.GetNamespaceUri("oslc_rm").ExtendLocalName("Requirement"); // "http://open-services.net/ns/rm#Requirement"
                graph.Assert(requirementNode, graph.CreateUriNode(rdfTypeUri), graph.CreateUriNode(requirementTypeUri));


                // Gereksinim özelliklerini RDF üçlüleri olarak ekleyin
                // Requirement sınıfındaki özellikleriniz ile OSLC RM/Dublin Core özellikleri arasındaki eşlemeyi yapın.

                // dcterms:identifier (Kimlik) URI'sini NamespaceMap üzerinden alma
                Uri dctermsIdentifierUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("identifier"); // "http://purl.org/dc/terms/identifier"
                // ID'nin boş olmadığını zaten yukarıda kontrol ettik, o yüzden burada doğrudan ekleyebiliriz.
                graph.Assert(requirementNode, graph.CreateUriNode(dctermsIdentifierUri), graph.CreateLiteralNode(req.Id));


                // dcterms:title (Başlık) URI'sini NamespaceMap üzerinden alma
                Uri dctermsTitleUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("title"); // "http://purl.org/dc/terms/title"
                if (!string.IsNullOrEmpty(req.Title)) // Başlık boş değilse ekle
                {
                    graph.Assert(requirementNode, graph.CreateUriNode(dctermsTitleUri), graph.CreateLiteralNode(req.Title));
                }
                // Başlık boşsa bile, zorunlu alanlar için boş literal eklemek bir seçenek olabilir, ancak genellikle boşluklar atlanır.
                // Eğer title zorunlu ise ve boş geliyorsa, bu bir veri kalitesi sorunudur.
                // graph.Assert(requirementNode, graph.CreateUriNode(dctermsTitleUri), graph.CreateLiteralNode(req.Title ?? "")); // Başlık boş olsa bile eklemek isterseniz


                // dcterms:description (Açıklama) URI'sini NamespaceMap üzerinden alma
                Uri dctermsDescriptionUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("description"); // "http://purl.org/dc/terms/description"
                // Açıklama null ise boş string kullan, böylece description triple'ı her zaman oluşur.
                // Not: Boş literal eklemek, açıklamanın olmadığı anlamına gelir. Eğer açıklama alanı girdide hiç yoksa, bu triple'ı hiç eklememek de bir seçenektir.
                // Ancak OSLC RM 2.0'da description zorunlu bir alan olarak belirtilmişse, boş literal eklemek uygun olabilir.
                graph.Assert(requirementNode, graph.CreateUriNode(dctermsDescriptionUri), graph.CreateLiteralNode(req.Description ?? ""));


                // dcterms:created (Oluşturma Tarihi) URI'sini NamespaceMap üzerinden alma - ZORUNLU ALAN
                Uri dctermsCreatedUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("created");
                // CreatedOn özelliği doluysa onu kullan, boşsa şimdiki zamanı kullan (fallback)
                DateTime createdDate = req.CreatedOn ?? DateTime.UtcNow; // UTC zamanı kullanmak iyi bir pratiktir

                // XSD datetime formatında literal oluştur
                Uri xsdDatetimeUri = new Uri(XmlSpecsHelper.XmlSchemaDataTypeDateTime);
                graph.Assert(requirementNode, graph.CreateUriNode(dctermsCreatedUri), graph.CreateLiteralNode(createdDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), xsdDatetimeUri));


                // oslc:instanceShape (Şema Referansı) URI'sini NamespaceMap üzerinden alma - ÖNEMLİ ALAN
                Uri oslcInstanceShapeUri = graph.NamespaceMap.GetNamespaceUri("oslc").ExtendLocalName("instanceShape");
                // Buraya gerçek OSLC RM Shape tanımınızın URI'sini ekleyin.
                // Örnek placeholder URI:
                Uri requirementShapeUri = new Uri(baseUri, "shapes/RequirementShape"); // Kendi Shape URI'nizi buraya koyun
                graph.Assert(requirementNode, graph.CreateUriNode(oslcInstanceShapeUri), graph.CreateUriNode(requirementShapeUri));


                // --- Yeni List<string> Özelliklerini RDF'e Ekleme (OSLC modelinize bağlı) ---
                // Bu kısım, List<string> içeriğini OSLC/RDF modeline nasıl eşleyeceğinize bağlıdır.
                // Aşağıdaki örnekler farklı yaklaşımları gösterir:

                // 1. Stakeholders (Örnek: dcterms:contributor olarak ekleme)
                // Genellikle OSLC'de ilişkiler URI'lerle ifade edilir.
                // Eğer stakeholder'lar başka sistemlerdeki kaynaklara karşılık geliyorsa, onların URI'lerini kullanmalısınız.
                // Burada basitçe her stakeholder adını bir literal olarak ekliyoruz.
                Uri dctermsContributorUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("contributor");
                if (req.Stakeholders != null)
                {
                    foreach (var stakeholder in req.Stakeholders)
                    {
                        if (!string.IsNullOrWhiteSpace(stakeholder))
                        {
                            // Her stakeholder adını ayrı bir literal olarak ekle
                            graph.Assert(requirementNode, graph.CreateUriNode(dctermsContributorUri), graph.CreateLiteralNode(stakeholder.Trim()));
                        }
                    }
                }

                // 2. Tags (Örnek: dcterms:subject veya skos:Concept olarak ekleme)
                // Genellikle etiketler SKOS (Simple Knowledge Organization System) Concept'lerine eşlenir.
                // Burada basitçe her etiketi bir literal olarak ekliyoruz.
                Uri dctermsSubjectUri = graph.NamespaceMap.GetNamespaceUri("dcterms").ExtendLocalName("subject");
                if (req.Tags != null)
                {
                    foreach (var tag in req.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            // Her etiket adını ayrı bir literal olarak ekle
                            graph.Assert(requirementNode, graph.CreateUriNode(dctermsSubjectUri), graph.CreateLiteralNode(tag.Trim()));
                            // Eğer etiketler URI'lere karşılık geliyorsa, UriNode kullanın:
                            // if (Uri.TryCreate(tag.Trim(), UriKind.Absolute, out Uri tagUri)) {
                            //     graph.Assert(requirementNode, graph.CreateUriNode(dctermsSubjectUri), graph.CreateUriNode(tagUri));
                            // }
                        }
                    }
                }

                // 3. Products (Örnek: oslc_rm:relatedResource veya kendi özel URI'niz ile ilişkilendirme)
                // Ürünler genellikle başka OSLC kaynaklarına (örn. Configuration Management'taki Configuration Item) karşılık gelir.
                // Eğer ürünlerinizin URI'leri varsa, oslc_rm:relatedResource gibi ilişkilerle bağlayın.
                // Burada basitçe her ürün adını bir literal olarak ekliyoruz.
                Uri oslcRmRelatedResourceUri = graph.NamespaceMap.GetNamespaceUri("oslc_rm").ExtendLocalName("relatedResource");
                // Kendi özel ürün ilişkisi URI'niz olabilir: Uri exProductUri = graph.NamespaceMap.GetNamespaceUri("ex").ExtendLocalName("relatedProduct");
                if (req.Products != null)
                {
                    foreach (var product in req.Products)
                    {
                        if (!string.IsNullOrWhiteSpace(product))
                        {
                            // Her ürün adını bir literal olarak ekle (veya URI'ye dönüştürüp UriNode kullanın)
                            graph.Assert(requirementNode, graph.CreateUriNode(oslcRmRelatedResourceUri), graph.CreateLiteralNode(product.Trim()));
                            // Eğer ürünler URI'lere karşılık geliyorsa:
                            // if (Uri.TryCreate(product.Trim(), UriKind.Absolute, out Uri productUri)) {
                            //     graph.Assert(requirementNode, graph.CreateUriNode(oslcRmRelatedResourceUri), graph.CreateUriNode(productUri));
                            // }
                        }
                    }
                }

                // 4. WorkPackages (Örnek: oslc_cm:jiraStory veya kendi özel URI'niz ile ilişkilendirme)
                // İş paketleri genellikle başka OSLC kaynaklarına (örn. Change Management'taki Change Request/Story) karşılık gelir.
                // Eğer iş paketlerinizin URI'leri varsa, oslc_rm:elaboratedBy, oslc_cm:relatedChangeRequest gibi ilişkilerle bağlayın.
                // Burada basitçe her iş paketi adını bir literal olarak ekliyoruz.
                Uri oslcRmElaboratedByUri = graph.NamespaceMap.GetNamespaceUri("oslc_rm").ExtendLocalName("elaboratedBy");
                // Kendi özel iş paketi ilişkisi URI'niz olabilir: Uri exWorkPackageUri = graph.NamespaceMap.GetNamespaceUri("ex").ExtendLocalName("relatedWorkPackage");
                if (req.WorkPackages != null)
                {
                    foreach (var workPackage in req.WorkPackages)
                    {
                        if (!string.IsNullOrWhiteSpace(workPackage))
                        {
                            // Her iş paketi adını bir literal olarak ekle (veya URI'ye dönüştürüp UriNode kullanın)
                            graph.Assert(requirementNode, graph.CreateUriNode(oslcRmElaboratedByUri), graph.CreateLiteralNode(workPackage.Trim()));
                            // Eğer iş paketleri URI'lere karşılık geliyorsa:
                            // if (Uri.TryCreate(workPackage.Trim(), UriKind.Absolute, out Uri wpUri)) {
                            //     graph.Assert(requirementNode, graph.CreateUriNode(oslcRmElaboratedByUri), graph.CreateUriNode(wpUri));
                            // }
                        }
                    }
                }


                // Requirement sınıfına eklediğiniz başka özellikler varsa, bunları da OSLC modeline uygun şekilde RDF üçlüleri olarak ekleyin.
            }

            // RDF grafını RDF/XML formatına serileştirin (yazın)
            var rdfXmlWriter = new RdfXmlWriter();
            rdfXmlWriter.PrettyPrintMode = true; // Daha okunaklı çıktı için

            // System.IO.StringWriter kullanarak tam nitelikli adı belirtme
            using (var stringWriter = new System.IO.StringWriter())
            {
                // WriteGraph metodunu kullanarak grafı StringWriter'a yazın
                rdfXmlWriter.Save(graph, stringWriter);

                // StringWriter'daki içeriği döndürün
                return stringWriter.ToString();
            }
        }
    }

    // dotNetRDF NamespaceMap.AddNamespace metodu için yardımcı extension metot
    // Bu metot, bir namespace URI'sine lokal bir isim ekleyerek tam URI oluşturur.
    public static class NamespaceMapperExtensions
    {
        public static Uri ExtendLocalName(this Uri baseUri, string localName)
        {
            // Namespace URI'sinin sonunda # veya / yoksa ekleyin (RDF/XML için yaygın kural)
            string uriString = baseUri.ToString();
            if (!uriString.EndsWith("#") && !uriString.EndsWith("/"))
            {
                // Eğer URI'nin sonunda dosya uzantısı gibi bir şey varsa # eklemek sorun yaratabilir.
                // Genellikle namespace URI'leri ya # ya da / ile biter.
                // Burada basitlik için # ekliyoruz, ancak gerçekte namespace tanımına bakmak daha doğru olur.
                uriString += "#";
            }
            return new Uri(uriString + localName);
        }
    }
}
