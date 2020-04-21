using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

class Program
{
    static void Main()
    {
        foreach (var fileResx in new DirectoryInfo(Environment.CurrentDirectory).GetFiles())
        {
            if (!string.Equals(fileResx.Extension, ".resx", StringComparison.InvariantCultureIgnoreCase)) 
                continue;

            var fileCSharp = new FileInfo(Regex.Replace(fileResx.FullName, @".resx$", ".Designer.cs", RegexOptions.IgnoreCase));
            
            var fileResxContent = File.ReadAllText(fileResx.FullName, Encoding.UTF8);
            var fileCSharpContent = fileCSharp.Exists ? File.ReadAllLines(fileCSharp.FullName, Encoding.UTF8) : new string[0];

            fileCSharpContent = SortCSharp(fileCSharpContent);
            fileResxContent = SortResx(fileResxContent);

            try
            {
                File.WriteAllText(fileResx.FullName, fileResxContent, Encoding.UTF8);
                File.WriteAllLines(fileCSharp.FullName, fileCSharpContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private enum Mode
    {
        Header,
        Body,
        Footer
    }

    private static string[] SortCSharp(string[] lines)
    {
        var header = new List<string>();
        var body = new List<List<string>>(); 
        var footer = new List<string>();

        var mode = Mode.Header;
        var i = 0;

        foreach (var line in lines)
        {
            switch (mode)
            {
                case Mode.Header:
                    if (!Regex.IsMatch(line, @"public\s*static\s*string\s*[^ ]*\s*{"))
                    {
                        header.Add(line);
                    }
                    else
                    {
                        body.Add(new List<string>());
                        
                        while (header[header.Count - 1].Trim() != "}")
                        {
                            body[body.Count - 1].Insert(0, header[header.Count - 1]);
                            header.RemoveAt(header.Count - 1);
                        }

                        mode = Mode.Body;
                        goto case Mode.Body;
                    }
                    
                    break;
                case Mode.Body:
                    if (line.Trim() == "}") i++;
                    else i = 0;

                    if (i <= 2)
                    {
                        body[body.Count - 1].Add(line);
                    }
                    if (i == 2)
                    {
                        body.Add(new List<string>());
                    }
                    
                    if (i > 2)
                    {
                        body.RemoveAt(body.Count - 1);
                        mode = Mode.Footer;
                        goto case Mode.Footer;
                    }
                    
                    break;
                case Mode.Footer:
                    footer.Add(line);
                    break;
            }
        }

        return Join(header, body, footer);
    }

    private static string SortResx(string content)
    {
        var xml = new XmlDocument();
        xml.LoadXml(content);
        var nodeParent = xml.ChildNodes[1];
        var nodesToSort = new List<XmlNode>();
        foreach (var node in nodeParent.Cast<XmlNode>().ToList())
        {
            var xmlElement = node as XmlElement;
            if (xmlElement == null || xmlElement.Name != "data") continue;
            nodeParent.RemoveChild(node);
            nodesToSort.Add(xmlElement);
        }

        var nodesSorted = nodesToSort.OrderBy(a => a.Attributes["name"].Value).ToList();
        foreach (var node in nodesSorted)
        {
            nodeParent.AppendChild(node);
        }

        using (var memoryStream = new MemoryStream())
        using (var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            xml.WriteContentTo(xmlTextWriter);
            xmlTextWriter.Flush();
            memoryStream.Flush();
            memoryStream.Position = 0;
            using (var streamReader = new StreamReader(memoryStream))
            {
                content = streamReader.ReadToEnd();
            }
        }

        return content;
    }

    private static string[] Join(List<string> header, List<List<string>> body, List<string> footer)
    {
        body.Sort((list1, list2) =>
        {
            const string regex = @"((?<=public\s*static\s*string\s*)[^ ]*(?=\s*{)|(?<=name="")[^""]*)";
            
            var text1 = string.Join(Environment.NewLine, list1.ToArray());
            var text2 = string.Join(Environment.NewLine, list2.ToArray());

            const string assemblyTag = "<assembly";
            var isTagAssembly1 = text1.Contains(assemblyTag);
            var isTagAssembly2 = text2.Contains(assemblyTag);

            if (isTagAssembly1 && !isTagAssembly2) return -1;
            if (!isTagAssembly1 && isTagAssembly2) return 1;

            const string attributeForText = " xml:space=\"";
            var isNotText1 = !text1.Contains(attributeForText);
            var isNotText2 = !text2.Contains(attributeForText);

            if (isNotText1 && !isNotText2) return 1;
            if (!isNotText1 && isNotText2) return -1;

            var key1 = Regex.Match(text1, regex, RegexOptions.Singleline).Value;
            var key2 = Regex.Match(text2, regex, RegexOptions.Singleline).Value;

            return string.Compare(key1, key2, StringComparison.InvariantCulture);
        });
        foreach (var block in body) header.AddRange(block);
        header.AddRange(footer);
        return header.ToArray();
    }
}