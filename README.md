# Catalyst.Modules.PluginManager
Catalyst.Modules.PluginManager

Loads DLL's via a specified plugin folder. This module will search for "plugin.json" inside all folders in the plugin path. plugin.json
contains infomation on what modules to load and module paramters if needed. Multiple modules can be loaded via 1 assembly plugin.json, if a module 
constructor contains a paramteter then it can be set also via the json file with the json key being the module class name 
as shown in the example. The PluginManager will also load all the DLL's inside the dictionaries where plugin.json exists.

<h1>Usage:</h1>

~~~
new PluginManagerModule("YOURPLUGINPATH")
~~~

<h1>Example:</h1>
1. Add Plugin Manager module 

~~~
new PluginManagerModule(@"C:\Users\J\.catalyst\Plugins")
~~~


2. Create plugin module folders (.e.g Authentication & Blazor Module)

![file1]


3. Build modules then copy all DLL's outputted by the modules inside the relevent folders


4. Add plugin.json inside the Authentication & Blazor folders
.e.g.

Authentication\plugin.json (one module, without ctor arguments):
~~~
{
	"AssemblyName": "Catalyst.Core.Modules.Authentication",
	"ModuleNames": ["AuthenticationModule"]
}
~~~

Blazor\plugin.json (multiple modules + ctor arguments):
~~~
{ 
   "AssemblyName":"Catalyst.Modules.Server.Blazor",
   "ModuleNames":[ 
      "BlazorServerModule, BlazorApiAgentModule"
   ],
   "BlazorServerModule":{ 
      "urlParameter":"Test",
      "aVar1":2
   },
   "BlazorApiAgentModule":{ 
      "apiAgent":{ 
         "apiUrl":"http://test.com",
         "apiPort":5555
      },
      "anyVar":2
   }
}
~~~

[file1]: https://i.imgur.com/6yjn2V8.png
