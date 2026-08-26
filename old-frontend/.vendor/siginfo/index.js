// Local stub replacing the unreachable "siginfo" package (registry proxy in
// this sandbox cannot fetch it). Only used transitively by vitest's optional
// "why-is-node-running" diagnostic helper, which we don't rely on.
module.exports = function siginfo() {
  return null;
};
