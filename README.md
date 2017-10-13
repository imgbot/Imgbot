# ImgBot

ImgBot crawls all your image files in GitHub and submits pull requests after applying a lossless compression.
This will make the file size go down, but leave the dimensions and quality just as good.

## The backend

The core of ImgBot runs on a serverless stack called [Azure Functions](https://azure.microsoft.com/en-us/services/functions/).
This is what is running the image compression, pushing commits, and opening Pull Requests.
Once you get the tools you need to work with Azure functions you can run the app locally from ImgBot.Function. 

You can either get the tools [integrated with Visual Studio](https://blogs.msdn.microsoft.com/webdev/2017/05/10/azure-function-tools-for-visual-studio-2017/) and use `F5` 
or you can [get the CLI](https://github.com/Azure/azure-functions-cli) standalone and use `func run ImgBot.Function`.
All operating systems welcome :)

Within the ImgBot.Function directory you will see the following key classes

 - `Functions.cs` - the triggers for starting the workflows
 - `CompressImages.cs` - clone the repo, compress the images, open the PR
 - `InstallationToken.cs` - uses a pem file and the GitHub API to get access to repos

*Note: For security reasons we cannot provide you with the pem file as this is a secret reserved for the production service. You can run every part of the function except parts where authentication is required without this secret. If you are working on a part of the function that requires this secret then you can generate one for yourself to test with. Register a GitHub app for development purpose and install this app into the repo you are using to test with. If there is a part of this process that isn't clear or you have any questions at all feel free to open an issue in this repo and we'll work it out :)*

## The frontend

The frontend that drives https://imgbot.net/ is a lightweight web app running on [ASP.NET Core](https://github.com/aspnet/Home) - Again, all operating systems welcome :)
The purpose of this website is to run the landing page for ImgBot as well as an endpoint for the webhooks from GitHub for installed repos.

Within the ImgBot.Web directory you will see the follwing key files

 - `Views/Home/Index.cshtml` - the landing page markup
 - `Views/Shared/_Layout.cshtml` - the layout for the landing page
 - `wwwroot/css/site.css` - the landing page stylesheet
 - `Controllers/HookController.cs` - the route for the webhooks
 
The 2 main events we deal with through webhooks are installation events and push events.
Each time a repo has ImgBot installed, GitHub fires a hook to `HookController.cs` and we start the installation workflow.
Each time a repo that already has ImgBot installed gets pushed to, GitHub fires a hook to `HookController.cs` and, if the commit contains an image update, we start the compression worflow.

## The end result

![](https://imgbot.net/images/screen.png)
