﻿<#@ template language="C#v3.5" debug="true" hostspecific="true" #>
<#@ output extension=".js" #>
<#@ assembly name="System.Core" #>
<#@ assembly name="Microsoft.VisualStudio.Shell.Interop.8.0" #>
<#@ assembly name="EnvDTE" #>
<#@ assembly name="EnvDTE80" #>
<#@ assembly name="VSLangProj" #>
<#@ assembly name="$(ProjectDir)$(OutDir)$(TargetFileName)" #>
<#@ assembly name="$(ProjectDir)$(OutDir)T4MvcJs.RoutesHandler.dll" #>
<#@ import namespace="System.Collections.Generic" #>
<#@ import namespace="System.IO" #>
<#@ import namespace="System.Linq" #>
<#@ import namespace="System.Text" #>
<#@ import namespace="System.Text.RegularExpressions" #>
<#@ import namespace="Microsoft.VisualStudio.Shell.Interop" #>
<#@ import namespace="EnvDTE" #>
<#@ import namespace="EnvDTE80" #>
<#@ import namespace="Microsoft.VisualStudio.TextTemplating" #>
<#@ assembly name="System.Web" #>
<#@ assembly name="System.Web.Mvc" #>
<#@ import namespace="System" #>
<#@ import namespace="System.Web" #>
<#@ import namespace="System.Web.Mvc" #>
<#@ import namespace="System.Web.Routing" #>
<#@ import namespace="System.Collections.Specialized" #>
<#@ import namespace="System.Reflection" #>
<# // To debug, uncomment the next two lines !! 
//System.Diagnostics.Debugger.Launch();
//System.Diagnostics.Debugger.Break();
#>
<#
PrepareDataToRender(this);  
SetupRoutes();
#>


var MvcJs = {
	<#
	var firstMvcJsElement = true;
	foreach (var area in Areas) {
		if (!firstMvcJsElement) {this.WriteLine(","); }

		PushIndent("\t");
		if (!string.IsNullOrEmpty(area.Name)) { 
			
			WriteLine("");
			WriteLine(area.Name + ": {");
			
			var firstElement = true;
			PushIndent("\t");
			
			RenderControllers(area.Controllers, ref firstElement);
			
			PopIndent();
			WriteLine("");
			WriteLine("}");
			
			
		} else {
			RenderControllers(area.Controllers, ref firstMvcJsElement);
		}
		
		PopIndent();
		firstMvcJsElement = false;
	}
	#>
};




<#@ Include File="T4MVC.source.t4" #>
<#+
static T4MvcJs.RoutesHandler.UrlGenerator UrlGenerator;
string _currentAssembly = new T4MvcJs.T4Proxy().GetType().AssemblyQualifiedName.Replace("T4MvcJs.T4Proxy", "");
	
void RenderConstants(ControllerInfo controller, ref bool firstElement) {
	var first = true;
	var childFound = false;
	var controllerType = System.Type.GetType(controller.FullClassName + _currentAssembly);
	if (controllerType == null)
		return;
	
	var controllerFields = controllerType.GetFields();
	foreach (var field in controllerFields) {
		if(field.IsLiteral && !field.IsInitOnly) {
			//this is constant

			childFound = true;
			var value = field.GetRawConstantValue();
			if (!firstElement) { WriteLine(","); }
			Write(field.Name + ": \"" + value + "\"");
			firstElement = false;
		}
	}
}

void RenderActionMethods(ControllerInfo controller, ref bool firstElement) {
	var methodNames = new List<string>();
	foreach (var method in controller.ActionMethods) {
		var parameters = "";
		var outputConditionals = "";
		var parameterNames = new List<string>();
		foreach (var methodParamInfo in method.Parameters)
		{
			var processed = false;
			//parse complex parameters
			/*if (methodParamInfo.Parameter.Type.TypeKind == EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType) {
				if (methodParamInfo.Parameter.Type.CodeType.InfoLocation != vsCMInfoLocation.vsCMInfoLocationExternal) {
					foreach (var modelProperty in methodParamInfo.Parameter.Type.CodeType.Children.OfType<CodeProperty>()) {
						AddParameter(modelProperty.Name, ref outputConditionals, ref parameters);
						parameterNames.Add(modelProperty.Name);
					}
					processed = true;
				}
			}*/

			if (!processed)
			{
				AddParameter(methodParamInfo.Name, ref outputConditionals, ref parameters);
				parameterNames.Add(methodParamInfo.Name);
			}
		}
		string url = "";
		try {
			url = UrlGenerator.GenerateUrl(controller.AreaName, controller.Name, method.Name, parameterNames.ToArray());
		} catch (Exception ex) {
			continue;
		}

		if (!firstElement) { this.WriteLine(","); }
	
		var jsMethodName = method.Name;
		var jsBaseMethodName = jsMethodName;
		var i = 1;
		while (methodNames.Contains(jsMethodName)) {
			jsMethodName = jsBaseMethodName + i;
			i++;
		}
		methodNames.Add(jsMethodName);
		
		WriteLine(jsMethodName + ": function(" + parameters + ") {");
		PushIndent("\t");
		WriteLine("var url = \""+url+"\";");
		WriteLine(outputConditionals);
		WriteLine("return url.replace(/([?&]+$)/g, \"\");");
		PopIndent();
		Write("}");
		firstElement = false;
	}
}

void RenderActionNames(ControllerInfo controller, ref bool firstElement) {
	if (!firstElement) {
		WriteLine(",");
	}
	firstElement = false;
	WriteLine("Actions: {");
	PushIndent("\t");
	var firstMethod = true;
	foreach (var method in controller.ActionMethods) {
		if (!firstMethod) { this.WriteLine(","); }
		Write(method.Name + ": \""+method.Name.ToLower()+"\"");
		firstMethod = false;
	}
	WriteLine("");
	PopIndent();
	Write("}");
}

void RenderControllers(IEnumerable<ControllerInfo> controllers, ref bool firstElement) {
	WriteLine("");
	foreach (var controller in controllers) { 
		if (!firstElement) {
			WriteLine(",");
		}

		WriteLine(controller.Name + ": {");
		PushIndent("\t");

		//WriteLine("Name: \"" + controller.Name + "\",");
		var first = true;
		
		//RenderActionNames(controller, ref first);
		RenderActionMethods(controller, ref first);
		RenderConstants(controller, ref first);
		
		WriteLine("");
		PopIndent();
		Write("}");
		firstElement = false;
	}
}


void AddParameter(string parameterName, ref string outputConditionals, ref string parameters) {
				if (parameters != "") { parameters += ", "; }
	            parameters += parameterName;
				outputConditionals += String.Format(@"
if ({0}) {{
	url = url.replace(""{{{0}}}"", {0});
}} else {{
	url = url.replace(""{0}={{{0}}}"", """").replace(""?&"",""?"").replace(""&&"",""&"");
}}
", parameterName, parameterName);

}

void SetupRoutes() {
	var routes = T4MvcJs.T4Proxy.GetRoutes();
	UrlGenerator = new T4MvcJs.RoutesHandler.UrlGenerator(routes);
}
#>