const azure = require('azure-storage');
const connectionstring = require('../connection'); // add connection.js with module.exports = "DefaultEndpointsProtocol=https;AccountName=XXX;AccountKey=XXX";
const queueService = azure.createQueueService(connectionstring);

const originQueue = 'compressimagesmessage-poison';
const destinationQueue = 'compressimagesmessage';

for (var i = 0; i < 50; i++) {
    queueService.getMessages(originQueue, (error, messages) => {
        if (error) {
        console.log(error);
        throw error;
        }

        queueService.createMessage(destinationQueue, messages[0].messageText, error => {
            if (error) {
                console.log(error);
                throw error;
            }

            queueService.deleteMessage(originQueue, messages[0].messageId, messages[0].popReceipt, error => {
                if (error) {
                    console.log(error);
                    throw error;
                }
            });
        });
    });
}
