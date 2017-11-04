All the code for ImgBot is available on GitHub. We will gladly accept contributions for the service, the website, and the documentation. This is where you can find out how to get set up to run locally as well as detailed information on exactly how ImgBot works.

### ImgBot Service

The core of ImgBot runs on a serverless stack called [Azure Functions](https://azure.microsoft.com/en-us/services/functions/).
The Function app is running the image compression, pushing the commits, and opening the Pull Requests.
Once you get the tools you need to work with Azure functions you can run the app locally from the ImgBot.Function directory. 

You can either get the tools [integrated with Visual Studio](https://blogs.msdn.microsoft.com/webdev/2017/05/10/azure-function-tools-for-visual-studio-2017/) and use `F5` 
or you can [get the CLI](https://github.com/Azure/azure-functions-cli) standalone and use `func run ImgBot.Function`.

The CLI is built with JavaScript so all operating systems are welcome :)

Azure Functions operate on top of storage. To run the function locally you will need to bring your own storage account and add a `local.settings.json` in the root with `AzureWebJobsStorage/AzureWebJobsDashboard` filled out. 

You can see the schema of this file in [the doc](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#local-settings-file)

Alternatively, running `func init` from the function directory will stamp out everything for you, or if you have a deployed function you can fetch the storage it is using.

`func azure functionapp fetch-app-settings <functionName>`

Now that you are running the service locally, within the ImgBot.Function directory you will see the following key classes

 - `Functions.cs` - the triggers for starting the workflows
 - `CompressImages.cs` - clones the repo, compresses the images, opens the PR
 - `InstallationToken.cs` - uses a pem file and the GitHub API to get access to repos
 - `Schedule.cs` - logic to limit the frequency of ImgBot PRs
 - `CommitMessage.cs` - generation of commit message compression report
 - `ImageQuery.cs` - Searches and extracts all the images out of the repo to be optimized
 - `LocalPath.cs` - Generates the location to clone from

#### Functions.cs

ImgBot uses [QueueTriggers](https://github.com/Azure/azure-webjobs-sdk/wiki/Queues#trigger) to kick off workflows.

The two triggers in place today are `imageupdatemessage` and `installationmessage`. Image update is the event that performs ongoing image optimizations for repos that already have ImgBot installed. Installation is the initial event for freshly installed repos.

This class is the entry point for the service. It is responsible for logging the installation in a storage table and kicking off the compression routine.

#### CompressImages.cs

ImgBot uses [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) to perform all the git operations. 

This class works through the following steps:

 1. Clones the repo
 2. Creates the 'imgbot' branch
 3. Runs the optimize routine 
 4. Commits the changes
 5. Pushes the 'imgbot' branch to the remote

The clone directory is read from environment variable `%TMP%`.

Once the branch is pushed, ImgBot uses [Octokit](https://github.com/octokit/octokit.net) to create the pull request in GitHub.

#### InstallationToken.cs

ImgBot uses [BouncyCastle](http://www.bouncycastle.org/csharp/) to help generate an [installation token](https://developer.github.com/apps/building-integrations/setting-up-and-registering-github-apps/about-authentication-options-for-github-apps/#authenticating-as-an-installation).

This class uses a combination of a Private Key, an App Id, and an InstallationId to generate a token to be used on behalf of the installation.

The benefit of using installation tokens is that the user is in full control of the permissions at all times. We never have to store any tokens and they expire after 10 minutes.

The installation token serves as a password to clone private repos, push to remotes, and open pull requests.

The username to accompany the installation token password is `x-access-token`.

For security reasons we cannot provide you with a pem file as this is a secret that delegates permissions in GitHub. You can run every part of the function except parts where authentication is required without this secret. If you are working on a part of the function that requires this secret then you can generate one for yourself to test with. [Register a GitHub app](https://github.com/settings/apps/new) for development purpose and install this app into the repo you are using to test with. Set the AppId and you should be good to go.

 If there is a part of this process that isn't clear or you have any questions at all feel free to open an issue in this repo and we'll work it out :)

#### Schedule.cs

This class is responsible for throttling optimization routines.
ImgBot is triggered when there is a new image added to a repo and by default will submit a PR as soon as it can.

Some users prefer to defer the pull requests and do the optimization in bigger batches. This is implemented by offering three options 'daily', 'weekly', and 'monthly'.

ImgBot will check the commit log to find the last time we committed an optimization. If it has been long enough since ImgBot has last committed in this branch then we will try to optimize the images again. Otherwise we will skip running optimizations for this run.

#### CommitMessage.cs

This class is responsible for generating a report of the image optimizations to be used in the commit message body.

The input is a dictionary where the key is the filename and the value is a pair of numbers that represent file size before and after compression.

This dictionary is transformed into an optimization report in the form of a commit message.

#### ImageQuery.cs

This class is responsible for locating all the images that are to be sent through the optimization routine.

The known image extensions are used to find all the images recursively and the ignored files from imgbotconfig are parsed.

#### LocalPath.cs

This class is responsible for generating the folder for the git operations to take place in.

Today this is done by combining the name of the repo with a random number.

### ImgBot website

The frontend that drives https://imgbot.net/ is a lightweight web app running on [ASP.NET Core](https://github.com/aspnet/Home). This framework runs on all operating systems :)

The purpose of this website is to run the landing page and docs for ImgBot as well as an endpoint for the webhooks from GitHub for installed repos.

The website uses bootstrap for a grid. To copy the lib files out of node_modules we use grunt (one time only).

```
npm run copy-libs
```

Within the ImgBot.Web directory you will see the following key files

 - `Views/Home/Index.cshtml` - the landing page markup
 - `Views/Shared/_Layout.cshtml` - the layout for the landing page
 - `wwwroot/css/site.less` - the landing page stylesheet
 - `Controllers/HookController.cs` - the route for the webhooks
 - `Controllers/HomeController.cs` - the routes for the landing page and the docs
 
The stylesheet is compiled using a grunt task mapped in the `package.json`.

```
npm run compile-less
```

The stylesheet can be compiled on save through grunt as well.

```
npm run watch
```

The 2 main events we deal with through webhooks are installation events and push events.
Each time a repo has ImgBot installed, GitHub fires a hook to `HookController.cs` and we start the installation workflow.

Each time a repo that already has ImgBot installed gets pushed to, GitHub fires a hook to `HookController.cs` and, if the commit contains an image update, we start the compression worflow.

If you need to make a connection to a real storage account you can update the `appsettings.json` locally to replace the `ACCOUNT_NAME` and `ACCOUNT_KEY` placeholders there. This should be the same storage account as the service is using for Azure Functions to enable message passing from the website to the service.

### ImgBot Docs

The docs are published from checked in markdown files to the [ImgBot website](https://imgbot.net/docs) to view in a browser. Alternatively, the docs can be browsed and edited [within GitHub](https://github.com/dabutvin/ImgBot/tree/master/Docs).

To work on the docs within the context of the website locally ImgBot uses a [gruntjs task](https://github.com/treasonx/grunt-markdown) to compile the markdown into HTML. To compile the docs locally run the following npm commands.

```
npm install
npm run compile-docs
``` 

To compile the markdown automatically on save

```
npm run watch
```

Check out the [gruntfile.js](https://github.com/dabutvin/ImgBot/tree/master/ImgBot.Web/gruntfile.js) and [package.json](https://github.com/dabutvin/ImgBot/tree/master/ImgBot.Web/package.json) to see how it is configured.

When the docs are compiled to HTML they are copied into the ImgBot.Web/Docs directory. In this directory there is a `metadata.json` file that will define the order and title of the doc.

```
{
    "slug": "contributing-website",
    "title": "Contribute to ImgBot website"
}
```

The slug matches the name of the markdown file and is also drives the URL segment to browse this doc.

This metadata file is read within the [HomeController.cs](https://github.com/dabutvin/ImgBot/tree/master/ImgBot.Web/Controllers/HomeController.cs) file.  The controller arranges all the documentation in memory in preparation for rendering.

The template that renders the documentation is [Docs.cshtml](https://github.com/dabutvin/ImgBot/tree/master/ImgBot.Web/Views/Home/Docs.cshtml). This is a razor file that renders the documentation navigation and content as HTML.

### Tests

ImgBot uses VSTest with NSubstitue to manage unit testing of the logic within the ImgBot codebase.
The tests live in the ImgBot.Test directory.
Please do your best to add tests as new features or logic are added so we can keep Imgbot running smoothly.
:)