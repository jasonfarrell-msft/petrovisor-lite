// Local stub delegating to Node's built-in "punycode" core module, since the
// standalone npm package of the same name is unreachable via the registry
// proxy in this sandbox.
module.exports = require('node:punycode');
