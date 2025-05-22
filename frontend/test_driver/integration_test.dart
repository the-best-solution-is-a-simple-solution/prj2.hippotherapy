import 'package:integration_test/integration_test_driver.dart';

// cmd to run the test
// start up chrome driver, then run the flutter drive cmd
// chromedriver --port=4444
// flutter drive --driver=test_driver/integration_test.dart --target=integration_test/create_patient_integration_test.dart -d chrome

Future<void> main() => integrationDriver();
