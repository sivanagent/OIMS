using System;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Text;
using System.Web.Services.Description;
using System.Xml;

//namespace Paradigma.Web.Services
namespace OIMS
{
    public class WebServiceInvoker
    {
        private Assembly webServiceAssembly;
        private List<string> services;
        Dictionary<string, Type> availableTypes;
        private NetworkCredential credentials;
        private Uri webServiceUri;

        /// <summary>
        /// Text description of the available services within this web service.
        /// </summary>
        public List<string> AvailableServices
        {
            get { return this.services; }
        }



        /// <summary>
        /// Creates the service invoker using the specified web service.
        /// </summary>
        /// <param name="webServiceUri"></param>
        public WebServiceInvoker(Uri webServiceUri) : this(webServiceUri, null)
        {
        }



        /// <summary>
        /// Creates the service invoker using the specified web service.
        /// </summary>
        /// <param name="webServiceUri">The web service URI.</param>
        /// <param name="credentials">The credentials.</param>
        public WebServiceInvoker(Uri webServiceUri, NetworkCredential credentials)
        {
            this.services = new List<string>(); // available services
            this.availableTypes = new Dictionary<string, Type>(); // available types
            this.credentials = credentials;
            this.webServiceUri = webServiceUri;

            // create an assembly from the web service description
            this.webServiceAssembly = BuildAssemblyFromWSDL(webServiceUri);

            // see what service types are available
            Type[] types = this.webServiceAssembly.GetExportedTypes();

            // and save them
            foreach (Type type in types)
            {
                if (type.Name.EndsWith("Client"))
                {
                    services.Add(type.FullName);
                    availableTypes.Add(type.FullName, type);
                }

            }

        }


        /// <summary>
        /// Gets a list of all methods available for the specified service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public List<string> EnumerateServiceMethods(string serviceName)
        {
            List<string> methods = new List<string>();
            if (!this.availableTypes.ContainsKey(serviceName))
                throw new Exception("Service Not Available");
            else
            {
                Type type = this.availableTypes[serviceName];

                // only find methods of this object type (the one we generated)
                // we don't want inherited members (this type inherited from SoapHttpClientProtocol)
                foreach (MethodInfo minfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    methods.Add(minfo.Name);
                return methods;
            }
        }


        /// <summary>
        /// Invokes the specified method of the named service.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="serviceName">The name of the service to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="args">The arguments to the method.</param>
        /// <returns>The return value from the web service method.</returns>
        public T InvokeMethod<T>(string serviceName, string methodName, params object[] args)
        {
            return (T)this.InvokeMethod(serviceName, methodName, args);
        }



        /// <summary>
        /// Invokes the specified method of the named service.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="serviceName">The name of the service to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="args">The arguments to the method.</param>
        /// <returns>The return value from the web service method.</returns>
        public object InvokeMethod(string serviceName, string methodName, params object[] args)
        {
            // create an instance of the specified service
            // and invoke the method
            System.ServiceModel.Channels.Binding defaultBinding = new BasicHttpBinding(BasicHttpSecurityMode.None);

            if (this.credentials != null)
            {
                ((BasicHttpBinding)defaultBinding).Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                ((BasicHttpBinding)defaultBinding).Security.Transport.ClientCredentialType = HttpClientCredentialType.Ntlm;
            }

            object obj = this.webServiceAssembly.CreateInstance(serviceName, false, BindingFlags.CreateInstance, null, new object[] { defaultBinding, new EndpointAddress(this.webServiceUri.ToString()) }, null, null);
            Type type = obj.GetType();

            if (this.credentials != null)
            {
                PropertyInfo piClientCreds = type.GetProperty("ClientCredentials");
                ClientCredentials creds = (ClientCredentials)piClientCreds.GetValue(obj, null);
                PropertyInfo piWindowsCreds = creds.GetType().GetProperty("Windows");
                WindowsClientCredential windowsCreds = (WindowsClientCredential)piWindowsCreds.GetValue(creds, null);
                PropertyInfo piAllowNtlm = windowsCreds.GetType().GetProperty("AllowNtlm");
                piAllowNtlm.SetValue(windowsCreds, true, null);
                PropertyInfo piCredentials = windowsCreds.GetType().GetProperty("ClientCredential");
                piCredentials.SetValue(windowsCreds, credentials, null);
                PropertyInfo piImpersonation = windowsCreds.GetType().GetProperty("AllowedImpersonationLevel");
                piImpersonation.SetValue(windowsCreds, System.Security.Principal.TokenImpersonationLevel.Impersonation, null);
            }

            return type.InvokeMember(methodName, BindingFlags.InvokeMethod, null, obj, args);
        }



        /// <summary>
        /// Builds the web service description importer, which allows us to generate a proxy class based on the 
        /// content of the WSDL described by the XmlTextReader.
        /// </summary>
        /// <param name="xmlreader">The WSDL content, described by XML.</param>
        /// <returns>A ServiceDescriptionImporter that can be used to create a proxy class.</returns>
        private ServiceDescriptionImporter BuildServiceDescriptionImporter(XmlTextReader xmlreader)
        {
            // make sure xml describes a valid wsdl
            if (!System.Web.Services.Description.ServiceDescription.CanRead(xmlreader))
                throw new Exception("Invalid Web Service Description");

            // parse wsdl
            System.Web.Services.Description.ServiceDescription serviceDescription = System.Web.Services.Description.ServiceDescription.Read(xmlreader);

            // build an importer, that assumes the SOAP protocol, client binding, and generates properties
            ServiceDescriptionImporter descriptionImporter = new ServiceDescriptionImporter();
            descriptionImporter.ProtocolName = "Soap12";
            descriptionImporter.AddServiceDescription(serviceDescription, null, null);
            descriptionImporter.Style = ServiceDescriptionImportStyle.Client;
            descriptionImporter.CodeGenerationOptions = System.Xml.Serialization.CodeGenerationOptions.GenerateProperties;

            return descriptionImporter;
        }



        private ServiceContractGenerator BuildServiceContractGenerator(XmlTextReader xmlreader)
        {
            // make sure xml describes a valid wsdl
            if (!System.Web.Services.Description.ServiceDescription.CanRead(xmlreader))
                throw new Exception("Invalid Web Service Description");

           // parse wsdl
            System.Web.Services.Description.ServiceDescription serviceDescription = System.Web.Services.Description.ServiceDescription.Read(xmlreader);
            MetadataSection section = MetadataSection.CreateFromServiceDescription(serviceDescription);
            MetadataSet metaDocs = new MetadataSet(new MetadataSection[] { section });
            WsdlImporter importer = new WsdlImporter(metaDocs);

            // Add any imported files
            foreach (System.Xml.Schema.XmlSchema wsdlSchema in serviceDescription.Types.Schemas)
            {
                foreach (System.Xml.Schema.XmlSchemaObject externalSchema in wsdlSchema.Includes)
                {
                    if (externalSchema is System.Xml.Schema.XmlSchemaImport)
                    {
                        Uri baseUri = webServiceUri;
                        if (string.IsNullOrEmpty(((System.Xml.Schema.XmlSchemaExternal)externalSchema).SchemaLocation)) continue;
                        Uri schemaUri = new Uri(baseUri, ((System.Xml.Schema.XmlSchemaExternal)externalSchema).SchemaLocation);
                        Console.WriteLine("== xml schema " + schemaUri);
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                        StreamReader sr = GetHttpWebResponse(schemaUri.ToString());
                        System.Xml.Schema.XmlSchema schema = System.Xml.Schema.XmlSchema.Read(sr, null);
                        importer.XmlSchemas.Add(schema);
                    }
                }
            }
            ServiceContractGenerator generator = new ServiceContractGenerator();
            IEnumerable<ContractDescription> contracts
                = importer.ImportAllContracts();

            importer.ImportAllEndpoints();
            foreach (ContractDescription contract in contracts)
            {
                generator.GenerateServiceContractType(contract);
            }

            if (generator.Errors.Count != 0)
                throw new Exception("There were errors during code compilation.");

            return generator;

        }



        /// <summary>
        /// Compiles an assembly from the proxy class provided by the ServiceDescriptionImporter.
        /// </summary>
        /// <param name="descriptionImporter"></param>
        /// <returns>An assembly that can be used to execute the web service methods.</returns>
        private Assembly CompileAssembly(ServiceContractGenerator serviceContractGenerator)
        {
            // a namespace and compile unit are needed by importer
            CodeNamespace codeNamespace = new CodeNamespace();
            CodeCompileUnit codeUnit = new CodeCompileUnit();

            codeUnit.Namespaces.Add(codeNamespace);

            // create a c# compiler
            CodeDomProvider compiler = CodeDomProvider.CreateProvider("CSharp");

            // include the assembly references needed to compile
            string[] references = new string[5] { "System.Web.Services.dll", "System.Xml.dll", "System.ServiceModel.dll", "System.configuration.dll", "System.Runtime.Serialization.dll" };
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            CompilerParameters parameters = new CompilerParameters(references);
            parameters.CompilerOptions = string.Format(@" /lib:{0}", string.Format("\"{0}\\Reference Assemblies\\Microsoft\\Framework\\v3.0\"", progFiles));

            // compile into assembly
            CompilerResults results = compiler.CompileAssemblyFromDom(parameters, serviceContractGenerator.TargetCompileUnit);
            foreach (CompilerError oops in results.Errors)
            {
                // trap these errors and make them available to exception object
                throw new Exception("Compilation Error Creating Assembly");
            }

            // all done....
            return results.CompiledAssembly;
        }



        /// <summary>
        /// Compiles an assembly from the proxy class provided by the ServiceDescriptionImporter.
        /// </summary>
        /// <param name="descriptionImporter"></param>
        /// <returns>An assembly that can be used to execute the web service methods.</returns>
        private Assembly CompileAssembly(ServiceDescriptionImporter descriptionImporter)
        {
            // a namespace and compile unit are needed by importer
            CodeNamespace codeNamespace = new CodeNamespace();
            CodeCompileUnit codeUnit = new CodeCompileUnit();

            codeUnit.Namespaces.Add(codeNamespace);
            ServiceDescriptionImportWarnings importWarnings = descriptionImporter.Import(codeNamespace, codeUnit);

            if (importWarnings == 0) // no warnings
            {
                // create a c# compiler
                CodeDomProvider compiler = CodeDomProvider.CreateProvider("CSharp");
                // include the assembly references needed to compile
                string[] references = new string[2] { "System.Web.Services.dll", "System.Xml.dll" };

                CompilerParameters parameters = new CompilerParameters(references);
                // compile into assembly
                CompilerResults results = compiler.CompileAssemblyFromDom(parameters, codeUnit);
                foreach (CompilerError oops in results.Errors)
                {
                    // trap these errors and make them available to exception object
                    throw new Exception("Compilation Error Creating Assembly");
                }

                // all done....
                return results.CompiledAssembly;
            }
            else
            {
                // warnings issued from importers, something wrong with WSDL
                throw new Exception("Invalid WSDL");
            }
        }

        /// <summary>
        /// Builds an assembly from a web service description.
        /// The assembly can be used to execute the web service methods.
        /// </summary>
        /// <param name="webServiceUri">Location of WSDL.</param>
        /// <returns>A web service assembly.</returns>
        private Assembly BuildAssemblyFromWSDL(Uri webServiceUri)
        {
            if (String.IsNullOrEmpty(webServiceUri.ToString()))
                throw new Exception("Web Service Not Found");
            StreamReader l_responseStream = GetHttpWebResponse(webServiceUri.ToString() + "?wsdl");
            XmlTextReader xmlreader = new XmlTextReader(l_responseStream);

            //ServiceDescriptionImporter descriptionImporter = BuildServiceDescriptionImporter(xmlreader);

            ServiceContractGenerator generator = BuildServiceContractGenerator(xmlreader);
            return CompileAssembly(generator);
            //return CompileAssembly(descriptionImporter);
        }



        private StreamReader GetHttpWebResponse(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new Exception("url Cannot be empty");

            HttpWebRequest l_http = (HttpWebRequest)WebRequest.Create(url);
            l_http.Timeout = 5000; // Timeout after 5 seconds
            if (this.credentials == null) l_http.UseDefaultCredentials = true;
            else l_http.Credentials = this.credentials;

            HttpWebResponse l_response = (HttpWebResponse)l_http.GetResponse();
            Encoding enc = Encoding.GetEncoding(1252); // Windows default Code Page

            return new StreamReader(l_response.GetResponseStream(), enc);
        }

    }



}
