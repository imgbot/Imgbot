After you install ImgBot into your repo, this is registered with GitHub and you are issued an *InstallationID*. ImgBot itself has an *ApplicationID* and a *private key* that is kept secret. 
With a combination of the *InstallationID*, the *ApplicationID*, and the *private key*, ImgBot can request an *Installation Token* from GitHub. 

This *Installation Token* is valid for 10 minutes at a time and provides the permissions agreed to at the time of installation. If the permissions required by ImgBot change you will be notified and have to accept before they can take effect.

ImgBot specifically uses this *Installation Token* to clone the repo, create a branch, push a commit to the branch, and open pull requests. These actions are performed on behalf of the installation with a username of 'x-access-token' and a password set to the value of the *Installation Token*.

The author and committer for these actions are set to `ImgBotApp<ImgBotHelp@gmail.com>`. 

Furthermore, the commits pushed by ImgBot are digitally signed with a secure and registered PGP private key and password and verified against a public key registered with GitHub. This verification provides an extra level of security to ensure that commits that come into your repo are in fact coming from the authentic ImgBot. And you get the nice green verified badge in the PR.

If at anytime you want to remove ImgBot's ability to interact with your repo, you simply uninstall the application from your settings and all further action's will be immediately blocked with no access.
