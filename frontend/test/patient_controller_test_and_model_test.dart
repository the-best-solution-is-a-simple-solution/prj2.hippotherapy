import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/patient_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:http/http.dart';

void main() {
  group('Test patient_controller.dart', () {
    test('Tests that guardian phone number is required for minors', () async {
      // Create a minor patient (age < 18)
      final Patient minorPatient = Patient(
        fName: 'Jane',
        lName: 'Doe',
        condition: 'Something',
        phone: '1234567890',
        age: 15,
        email: 'jane.doe@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // No guardian phone number set
      minorPatient.guardianPhoneNumber = null;

      // Check if guardianPhoneNumber is required for minors (age < 18)
      // should return an Error 400, bad request
      try {
        final failedRes =
            await PatientController.postPatientToDatabase(minorPatient);
        fail("Expected exception, but got: ${failedRes.statusCode}\n"
            "${failedRes.body}");
      } catch (e) {
        expect(e.toString(), contains('"status":400'));
        expect(
            e.toString(),
            contains(
                '"Guardian phone number is required when the age is below 18"'));
      }

      // Add guardian phone number for the minor patient
      minorPatient.guardianPhoneNumber = '123-456-7890';

      final successRes =
          await PatientController.postPatientToDatabase(minorPatient);

      //check that now I was able to submit it
      expect(successRes.statusCode, HttpStatus.ok);
    });

    test('Test that invalid phone number throws error', () async {
      // Create a patient with an invalid phone number
      final Patient invalidPatient = Patient(
        fName: 'Invalid',
        lName: 'Phone',
        condition: 'None',
        phone: 'invalid-phone',
        age: 30,
        email: 'invalid.phone@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if the phone number is invalid I do get an exception from the
      // controller in the front-end - other test above will show the exception
      // is actually a 422 error
      expect(() => PatientController.postPatientToDatabase(invalidPatient),
          throwsException);
    });

    test('Test that valid phone number does not throw error', () async {
      // Create a patient with a valid phone number
      // but no guardian phone number is required for >18
      final Patient validPatient = Patient(
        fName: 'Valid',
        lName: 'Phone',
        condition: 'None',
        phone: '123-456-7890',
        age: 30,
        email: 'valid.phone@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if no exception is thrown for valid phone number
      final res = await PatientController.postPatientToDatabase(validPatient);
      expect(res.statusCode, HttpStatus.ok);
    });

    test('Test that empty fields throw error', () async {
      // Create a patient with an empty first name
      Patient invalidPatient = Patient(
        fName: '',
        lName: 'Doe',
        condition: 'Something',
        phone: '123-456-7890',
        age: 30,
        email: 'empty@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if an empty first name triggers validation error
      expect(() => PatientController.postPatientToDatabase(invalidPatient),
          throwsException);

      // Create a patient with an empty last name
      invalidPatient = Patient(
        fName: 'John',
        lName: '',
        condition: 'Healthy',
        phone: '123-456-7890',
        age: 30,
        email: 'empty@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if an empty last name triggers validation error
      expect(() => PatientController.postPatientToDatabase(invalidPatient),
          throwsException);
    });

    test('Test that valid email is required', () async {
      // Create a patient with an invalid email
      final Patient invalidEmailPatient = Patient(
        fName: 'Invalid',
        lName: 'Email',
        condition: 'None',
        phone: '123-456-7890',
        age: 30,
        email: 'invalid-email',
        doctorPhoneNumber: '0987654321',
      );

      // Check if invalid email triggers an error
      expect(() => PatientController.postPatientToDatabase(invalidEmailPatient),
          throwsException);

      // Create a patient with a valid email
      final Patient validEmailPatient = Patient(
        fName: 'Valid',
        lName: 'Email',
        condition: 'Healthy',
        phone: '123-456-7890',
        age: 30,
        email: 'valid.email@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if no exception is thrown for valid email
      expect(() => PatientController.postPatientToDatabase(validEmailPatient),
          returnsNormally);
    });

    test('Test that age cannot be negative', () async {
      // Create a patient with negative age
      final Patient invalidAgePatient = Patient(
        fName: 'Invalid',
        lName: 'Age',
        condition: 'None',
        phone: '123-456-7890',
        age: -1,
        email: 'invalid.age@example.com',
        doctorPhoneNumber: '0987654321',
      );

      // Check if a negative age triggers validation error
      expect(() => PatientController.postPatientToDatabase(invalidAgePatient),
          throwsException);
    });

    test('Test that deletePatientByID throws error if patient does not exist',
        () async {
      try {
        final res =
            await PatientController.archivePatientByID('non-existent-id');
        fail('Expected exception, but got: ${res.statusCode}\n${res.body}');
      } catch (e) {
        expect(e.toString(), contains('Patient was not found.'));
      }
    });

    test('Test that modifyPatientByID successfully updates a patient',
        () async {
      // Insert a patient into the database
      final Patient testPatient = Patient(
        fName: 'Aron',
        lName: 'LastName',
        condition: 'Something',
        phone: '1234567890',
        age: 15,
        email: 'aron@domain.com',
        doctorPhoneNumber: '098-765-4321',
        guardianPhoneNumber: '639-175-0645',
      );

      final Response res =
          await PatientController.postPatientToDatabase(testPatient);
      final Map<String, dynamic> responseBody = json.decode(res.body);

      // Ensure the response body contains the patient's ID
      // also ensure that the ID is attached to the patient
      final String patientId = responseBody['id'];
      expect(patientId, isNotEmpty);

      testPatient.id = patientId;

      // Step 2: Modify the patient's data using the existing ID from the inserted patient
      testPatient.fName = 'UpdatedAron';
      testPatient.lName = 'UpdatedLastName';
      testPatient.age = 16;

      final Response updateRes =
          await PatientController.modifyPatientByID(testPatient);

      // Step 3: Check if the update response is successful
      expect(updateRes.statusCode, HttpStatus.ok);

      final Map<String, dynamic> updatedResponseBody =
          json.decode(updateRes.body);
      expect(updatedResponseBody['message'], 'Patient Updated Successfully');
      expect(updatedResponseBody['id'], patientId);

      // Step 4: Fetch the patient by ID and verify the changes
      final Patient updatedPatient =
          await PatientController.getPatientByID(patientId);

      expect(updatedPatient.fName, 'UpdatedAron');
      expect(updatedPatient.lName, 'UpdatedLastName');
      expect(updatedPatient.age, 16);
    });

    test('Test that modifyPatientByID throws error if invalid data is passed',
        () async {
      // Step 1: Insert a valid patient first
      final Patient testPatient = Patient(
        fName: 'Aron',
        lName: 'LastName',
        condition: 'Something',
        phone: '1234567890',
        age: 15,
        email: 'aron@domain.com',
        doctorPhoneNumber: '098-765-4321',
        guardianPhoneNumber: '639-175-0645',
      );

      final Response res =
          await PatientController.postPatientToDatabase(testPatient);
      final Map<String, dynamic> responseBody = json.decode(res.body);
      final String patientId = responseBody['id'];

      expect(patientId, isNotEmpty);
      // Modify the patient's data with invalid values
      testPatient.id = patientId;
      testPatient.fName = ''; // Invalid first name
      testPatient.lName = 'UpdatedLastName';

      try {
        final updateRes =
            await PatientController.modifyPatientByID(testPatient);
        fail(
            'Expected exception, but got: ${updateRes.statusCode}\n${updateRes.body}');
      } catch (e) {
        // expect the 400 status code
        expect(e.toString(), contains('400'));
      }
    });

    test('Test that modifyPatientByID throws error if patient does not exist',
        () async {
      // Step 1: Insert a valid patient first
      final Patient testPatient = Patient(
        fName: 'Aron',
        lName: 'LastName',
        condition: 'Something',
        phone: '1234567890',
        age: 15,
        email: 'aron@domain.com',
        doctorPhoneNumber: '098-765-4321',
        guardianPhoneNumber: '639-175-0645',
      );

      final Response res =
          await PatientController.postPatientToDatabase(testPatient);
      final Map<String, dynamic> responseBody = json.decode(res.body);
      final String patientId = responseBody['id'];

      // Trying to update a non-existent patient (use an invalid ID)
      testPatient.fName = 'UpdatedAron';
      testPatient.lName = 'UpdatedLastName';
      testPatient.age = 16;

      try {
        // Use an invalid ID to test the non-existent patient error
        testPatient.id = 'non-existent-id';
        final updateRes =
            await PatientController.modifyPatientByID(testPatient);
        fail(
            'Expected exception, but got: ${updateRes.statusCode}\n${updateRes.body}');
      } catch (e) {
        expect(
            e.toString(),
            contains(
                'Exception: Failed to add the patient Patient with non-existent-id not found'));
      }

      // Try modifying the actual patient again
      testPatient.id = patientId;
      final updateRes = await PatientController.modifyPatientByID(testPatient);

      expect(updateRes.statusCode, HttpStatus.ok);
    });

    test('Test that deletePatientByID successfully deletes a patient',
        () async {
      // Insert a patient into the database
      final Patient testPatient = Patient(
        fName: 'Aron',
        lName: 'LastName',
        condition: 'Something',
        phone: '1234567890',
        age: 15,
        email: 'aron@domain.com',
        doctorPhoneNumber: '098-765-4321',
        guardianPhoneNumber: '639-175-0645',
      );

      final Response res =
          await PatientController.postPatientToDatabase(testPatient);
      final Map<String, dynamic> responseBody = json.decode(res.body);
      final String patientId = responseBody['id'];
      expect(patientId, isNotEmpty);

      // Delete the patient by ID
      final Response deleteRes =
          await PatientController.archivePatientByID(patientId);

      // Check if the delete response is successful
      expect(deleteRes.statusCode, HttpStatus.ok);
      final Map<String, dynamic> deleteResponseBody =
          json.decode(deleteRes.body);
      expect(deleteResponseBody['message'], 'Patient archived successfully.');

      // Try fetching the patient after deletion to confirm it no longer exists
      try {
        await PatientController.getPatientByID(patientId);
        fail('Expected exception when fetching a deleted patient');
      } catch (e) {
        expect(e.toString(), contains('Cannot access an archived patient'));
      }
    });
  });
}
