const core = require('@actions/core');
const github = require('@actions/github');
const cli = require('@actions/exec');
const io = require('@actions/io');

try {
    // `who-to-greet` input defined in action metadata file
    // const nameToGreet = core.getInput('who-to-greet');

    console.log(`Hello! This is the console.log()`);

    const time = (new Date()).toTimeString();

    core.setOutput("result", `Howdy @ ${time}! This is the output result`);

    // Get the JSON webhook payload for the event that triggered the workflow
    const payload = JSON.stringify(github.context.payload, undefined, 2);
    const authToken = core.getInput('auth-token');
    const envVars = JSON.stringify(process.env,null,2);

    console.log(`Token Snippet: ${authToken.substr(0, 4)}-${authToken.substr(authToken.length - 4)}\nThe event payload:\n${payload}\nprocess.env: ${envVars}`);
} catch (error) {
    core.setFailed(`Failed: ${error.message}`);
}