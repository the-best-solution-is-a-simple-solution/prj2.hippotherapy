# frontend

A new Flutter project.

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Lab: Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Cookbook: Useful Flutter samples](https://docs.flutter.dev/cookbook)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.

## Testing

Start chrome driver

```bash
chromedriver --port=4444
```

- Install the "Bash Scripts Support" plugin.
- Right click "all_test_chrome.bat" and select 'Run...' to open chrome and see tests run.
- Right click "all_test_headless.bat" and select 'Run...' to run only in terminal.

IMPORTANT! - Remember to start up the backend with args = test (to load test data on test port)

From the root of the project, run the following command:

(For running on linux)

```bash
flutter drive \
--driver=test_driver/integration_test.dart \
--target=integration_test/all_tests.dart \
-d chrome
```

To run headless

```bash
flutter drive \
--driver=test_driver/integration_test.dart \
--target=integration_test/all_tests.dart \
-d web-server
```

### Install Instructions

Follow this guide: https://docs.flutter.dev/testing/integration-tests#test-in-a-web-browser
How to add to path in Windows: https://windowsloop.com/how-to-add-to-windows-path/

### Sources

https://docs.flutter.dev/testing/integration-tests#test-in-a-web-browser
https://github.com/flutter/flutter/blob/main/packages/integration_test/README.md
https://windowsloop.com/how-to-add-to-windows-path/