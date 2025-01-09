# TODOs for mcpdotnet

## Integration Testing
- [ ] Add comprehensive Roots feature testing once reference servers implement support
  - (Contribute PR to everything server adding `listRoots` tool)
  - Add integration tests similar to Sampling tests
  - Verify roots notification handling

## Future Improvements
- [ ] Add HTTPS/SSE transport support

## Code Improvements
- [ ] Consolidate notification handling in McpClient to reduce duplication between SendNotificationAsync overloads