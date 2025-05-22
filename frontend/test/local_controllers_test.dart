import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/export_controller.dart';
import 'package:frontend/controllers/local_controllers/local_patient_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/therapist.dart';
import 'package:http/http.dart' as http;

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  String address;
  String port;
  String testsController;

  final Therapist guestTherapist = Therapist(
      email: "guest1@test.com", fName: "Guest", lName: "Local");

  final Patient patientOne = Patient(
      therapistId: guestTherapist.therapistID,
      id: "local-patient1-id",
      fName: "Local",
      lName: "PatientOne",
      condition: "Stroke",
      phone: "123-123-1111",
      age: 31,
      email: "local1@test.com",
      doctorPhoneNumber: "123-123-1235");

  const FlutterSecureStorage storage = FlutterSecureStorage();


  group('local patient controller tests', () {
    final LocalPatientController patientController = LocalPatientController();

    setUpAll(() async {
      //storage.deleteAll();
    });

    test('Test save patient succeeds', () async {
      Patient patient = patientOne;


      LocalPatientController.savePatient(patient);
      final result = await storage.read(key: patient.id!);
      print(result);
      expect(result, patient.toJson().toString());
      // check it exists


      // final List<List<String>> listOfRecords =
      // await ExportController.fetchFilteredRecords("John Smith", "", "", "");
      //
      // for (final e in listOfRecords) {
      //   expect(e[0], "John Smith");
      // }
      // expect(listOfRecords, isNotEmpty);
      // expect(listOfRecords.length, 2);
    });

    test('Tests that gets all records by condition', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          null, "Autism", null, null);
      for (final e in listOfRecords) {
        expect(e[4], "Autism");
      }
      expect(listOfRecords, isNotEmpty);
    });

    test('Tests that gets all records by location', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(null, null, "NA", null);
      for (final e in listOfRecords) {
        expect(e[6], "NA");
      }
      expect(listOfRecords, isNotEmpty);
    });

    test('Tests that gets all records by dateTime', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          null, null, null, "2021-11-20,2021-11-21");
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(
            DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")), true);
        expect(
            DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")), true);
      }
    });

    test('Tests that gets all records by name and condition', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          "John Smith", "Autism", null, null);
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[0], "John Smith");
        expect(e[4], "Autism");
      }
    });

    test('Tests that gets all records by condition and dateTime', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          null, "Autism", null, "2021-11-20,2021-11-21");
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[4], "Autism");
        expect(
            DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")), true);
        expect(
            DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")), true);
      }
    });

    test('Tests that gets all records by NAnd dateTime', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          null, null, "NA", "2021-11-20,2021-11-21");
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[6], "NA");
        expect(
            DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")), true);
        expect(
            DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")), true);
      }
    });

    test('Tests that gets all records by name, condition, and location',
            () async {
          final List<List<String>> listOfRecords =
          await ExportController.fetchFilteredRecords(
              "John Smith", "Autism", "NA", null);
          expect(listOfRecords, isNotEmpty);
          for (final e in listOfRecords) {
            expect(e[0], "John Smith");
            expect(e[4], "Autism");
            expect(e[6], "NA");
          }
        });

    test('Tests that gets all records by name, condition, and dateTime',
            () async {
          final List<List<String>> listOfRecords =
          await ExportController.fetchFilteredRecords(
              "John Smith", "Autism", null, "2021-11-20,2021-11-21");
          expect(listOfRecords, isNotEmpty);
          for (final e in listOfRecords) {
            expect(e[0], "John Smith");
            expect(e[4], "Autism");
            expect(
                DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")),
                true);
            expect(
                DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")),
                true);
          }
        });

    test('Tests that gets all records by name, location, and dateTime',
            () async {
          final List<List<String>> listOfRecords =
          await ExportController.fetchFilteredRecords(
              "John Smith", null, "NA", "2021-11-20,2021-11-21");
          expect(listOfRecords, isNotEmpty);
          for (final e in listOfRecords) {
            expect(e[0], "John Smith");
            expect(e[6], "NA");
            expect(
                DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")),
                true);
            expect(
                DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-21")),
                true);
          }
        });

    test('Tests that gets all records by condition, location, and dateTime',
            () async {
          final List<List<String>> listOfRecords =
          await ExportController.fetchFilteredRecords(
              null, "Autism", "NA", "2021-11-20,2021-11-21");
          expect(listOfRecords, isNotEmpty);
          for (final e in listOfRecords) {
            expect(e[4], "Autism");
            expect(e[6], "NA");
            expect(
                DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")),
                true);
            expect(
                DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")),
                true);
          }
        });

    test('Tests that gets all records by all parameters', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          "John Smith", "Autism", "NA", "2021-11-20,2021-11-21");
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[0], "John Smith");
        expect(e[4], "Autism");
        expect(e[6], "NA");
        expect(
            DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")), true);
        expect(
            DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")), true);
      }
    });

    test('Tests that gets all records by name and location', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          "John Smith", null, "NA", null);
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[0], "John Smith");
        expect(e[6], "NA");
      }
    });

    test('Tests that gets all records by name and dateTime', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          "John Smith", null, null, "2021-11-20,2021-11-21");
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[0], "John Smith");
        expect(
            DateTime.parse(e[5]).isAfter(DateTime.parse("2021-11-19")), true);
        expect(
            DateTime.parse(e[5]).isBefore(DateTime.parse("2021-11-22")), true);
      }
    });

    test('Tests that gets all records by condition and location', () async {
      final List<List<String>> listOfRecords =
      await ExportController.fetchFilteredRecords(
          null, "Autism", "NA", null);
      expect(listOfRecords, isNotEmpty);
      for (final e in listOfRecords) {
        expect(e[4], "Autism");
        expect(e[6], "NA");
      }
    });
  });
}
