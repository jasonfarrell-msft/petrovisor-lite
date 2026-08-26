// Local stub replacing the unreachable "stackback" package (registry proxy
// in this sandbox cannot fetch it). Only used transitively by vitest's
// optional "why-is-node-running" diagnostic helper, which we don't rely on.
module.exports = function stackback(err) {
  return (err && err.stack) ? err.stack.split('\n') : [];
};
