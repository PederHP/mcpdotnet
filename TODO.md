# TODOs for mcpdotnet

## Integration Testing
- Add comprehensive Roots feature testing once reference servers implement support
  - Consider using everything server adding `listRoots` tool)
- Add integration tests similar to Sampling tests
- Verify roots notification handling
- Expand SSE test server to support all features or use a reference SSE server if one becomes available

## Future Improvements
- [X] Add HTTPS/SSE transport support
- [ ] Add more example applications showing different capabilities
- [ ] Add comprehensive documentation for advanced scenarios
- [ ] Profile and optimize performance
- [ ] Linux support in stdio transport

## Code Improvements
- [ ] Consolidate notification handling in McpClient to reduce duplication between SendNotificationAsync overloads