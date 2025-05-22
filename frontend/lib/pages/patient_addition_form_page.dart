import 'dart:convert';
import 'dart:math';

import 'package:emoji_picker_flutter/emoji_picker_flutter.dart';
import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_patient_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:provider/provider.dart';

class PatientAdditionFormPage extends StatefulWidget {
  final Patient? patient;

  const PatientAdditionFormPage({super.key, this.patient});

  @override
  PatientAdditionFormPageState createState() => PatientAdditionFormPageState();
}

class PatientAdditionFormPageState extends State<PatientAdditionFormPage> {
  final _formKey = GlobalKey<FormBuilderState>();

  // Initialize temporary patient. If updating, use passed-in patient, otherwise initialize with empty values.
  late Patient _tempFormPatient;

  // generate an random emoji from the food category to assign
  // to patient.
  String getRandomFoodEmoji() {
    final List<Emoji> foodEmojis = defaultEmojiSet
        .where((emoji) => emoji.category == Category.FOODS)
        .first
        .emoji;

    final randomIndex = Random().nextInt(foodEmojis.length);
    return foodEmojis[randomIndex].emoji;
  }

  @override
  void initState() {
    super.initState();

    final originalPassedInPatient = widget.patient;
    final authController = Provider.of<AuthController>(context, listen: false);

    // Initialize temporary patient for only the required fields
    _tempFormPatient = originalPassedInPatient ??
        Patient(
            fName: '',
            lName: '',
            condition: '',
            phone: '',
            age: 0,
            email: '',
            doctorPhoneNumber: '',
            therapistId: authController.therapistId ?? '',
            emoji: getRandomFoodEmoji());
  }

  @override
  Widget build(final BuildContext context) {
    AuthController _authController = AuthController();

    // Check if a patient is passed to the form (update mode)
    final patient = widget.patient;
    final isUpdateForm = patient != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(isUpdateForm
            ? 'Update ${patient.fName} ${patient.lName}'
            : 'Create New Patient'),
      ),
      body: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: FormBuilder(
            key: _formKey,
            initialValue: isUpdateForm
                ? {
                    'first_name': patient.fName.trim(),
                    'last_name': patient.lName.trim(),
                    'condition': patient.condition.trim(),
                    'phone': patient.phone.trim(),
                    'age': patient.age.toString().trim(),
                    'email': patient.email.trim(),
                    'doctor_phone': patient.doctorPhoneNumber.trim(),
                    'weight': patient.weight?.toString().trim(),
                    'height': patient.height?.toString().trim(),
                    'guardian_phone': patient.guardianPhoneNumber?.trim(),
                  }
                : {},
            child: Column(
              children: [
                // First Name (Required, Min: 2, Max: 50)
                FormBuilderTextField(
                  key: const Key('first_name'),
                  name: 'first_name',
                  decoration: const InputDecoration(labelText: 'First Name'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'First name is required'),
                    FormBuilderValidators.minLength(2,
                        errorText:
                            'First name must be at least 2 characters long'),
                    FormBuilderValidators.maxLength(50,
                        errorText:
                            'First name must be at most 50 characters long'),
                    FormBuilderValidators.alphabetical(
                        errorText: 'First name must be alphanumeric characters',
                        regex: RegExp(r'^[a-zA-Z ]+$'))
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.fName = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Last Name (Required, Min: 2, Max: 50)
                FormBuilderTextField(
                  key: const Key('last_name'),
                  name: 'last_name',
                  decoration: const InputDecoration(labelText: 'Last Name'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Last name is required'),
                    FormBuilderValidators.minLength(2,
                        errorText:
                            'Last name must be at least 2 characters long'),
                    FormBuilderValidators.maxLength(50,
                        errorText:
                            'Last name must be at most 50 characters long'),
                    FormBuilderValidators.alphabetical(
                        errorText: 'Last name must be alphanumeric character',
                        regex: RegExp(r'^[a-zA-Z ]+$'))
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.lName = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Condition (Required, Min: 2, Max: 50)
                FormBuilderTextField(
                  key: const Key('condition'),
                  name: 'condition',
                  decoration: const InputDecoration(labelText: 'Condition'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Condition is required'),
                    FormBuilderValidators.minLength(2,
                        errorText:
                            'Condition must be at least 2 characters long'),
                    FormBuilderValidators.maxLength(50,
                        errorText:
                            'Condition must be at most 50 characters long'),
                    FormBuilderValidators.alphabetical(
                        errorText: 'Condition must be alphanumeric characters',
                        regex: RegExp(r'^[a-zA-Z -]+$'))
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.condition = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Phone (Valid phone number format)
                FormBuilderTextField(
                  key: const Key('phone'),
                  name: 'phone',
                  decoration: const InputDecoration(labelText: 'Phone Number'),
                  keyboardType: TextInputType.phone,
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Phone number is required'),
                    FormBuilderValidators.phoneNumber(
                        errorText: 'Invalid phone number format')
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.phone = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Age (Required, Between 1 and 100)
                FormBuilderTextField(
                  key: const Key('age'),
                  name: 'age',
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Age'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Age is required'),
                    FormBuilderValidators.integer(
                        errorText: 'Age must be numeric characters', radix: 10),
                    FormBuilderValidators.min(1,
                        errorText: 'Age must be at least 1'),
                    FormBuilderValidators.max(100,
                        errorText: 'Age must be at most 100')
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.age = int.tryParse(value ?? '') ?? 0;
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Email (Required, Valid email)
                FormBuilderTextField(
                  key: const Key('email'),
                  name: 'email',
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(labelText: 'Email'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Email is required'),
                    FormBuilderValidators.email(
                        errorText: 'Must be a valid email address'),
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.email = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Doctor Phone (Required, Valid phone)
                FormBuilderTextField(
                  key: const Key('doctor_phone'),
                  name: 'doctor_phone',
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                      labelText: 'Patient\'s Doctor\'s Phone Number'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.required(
                        errorText: 'Doctor phone is required'),
                    FormBuilderValidators.phoneNumber(
                        errorText: 'Invalid doctor phone number')
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.doctorPhoneNumber = value?.trim() ?? '';
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Weight (Optional, but if provided must be between 20kg and 300kg)
                FormBuilderTextField(
                  key: const Key('weight'),
                  name: 'weight',
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Weight (kg)'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.numeric(
                        errorText: 'Weight must be numeric characters',
                        checkNullOrEmpty: false),
                    FormBuilderValidators.min(20,
                        errorText: 'Weight must be between 20kg and 300kg',
                        checkNullOrEmpty: false),
                    FormBuilderValidators.max(300,
                        errorText: 'Weight must be between 20kg and 300kg',
                        checkNullOrEmpty: false),
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.weight =
                          value != null ? double.tryParse(value) : null;
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Height (Optional, but if provided must be between 50cm and 300cm)
                FormBuilderTextField(
                  key: const Key('height'),
                  name: 'height',
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(labelText: 'Height (cm)'),
                  validator: FormBuilderValidators.compose([
                    FormBuilderValidators.numeric(
                        errorText: 'Height must be numeric characters',
                        checkNullOrEmpty: false),
                    FormBuilderValidators.min(50,
                        errorText: 'Height must be between 50cm and 300cm',
                        checkNullOrEmpty: false),
                    FormBuilderValidators.max(300,
                        errorText: 'Height must be between 50cm and 300cm',
                        checkNullOrEmpty: false),
                  ]),
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.height =
                          value != null ? double.tryParse(value) : null;
                    });
                  },
                ),
                const SizedBox(height: 10),

                // Guardian Phone (Required if under 18)
                FormBuilderTextField(
                  key: const Key('guardian_phone'),
                  name: 'guardian_phone',
                  decoration: const InputDecoration(
                      labelText: 'Guardian\'s Phone Number'),
                  validator: (final val) {
                    if (_tempFormPatient.age >= 18) {
                      return null;
                    }
                    // If patient is under 18, guardian phone is required
                    if (_tempFormPatient.age < 18 &&
                        (val == null || RegExp(r'^\s*$').hasMatch(val))) {
                      return 'Guardian phone is required for minors';
                    }
                    // Validate phone number format if provided
                    return FormBuilderValidators.phoneNumber(
                            errorText: 'Invalid guardian phone number')
                        .call(val);
                  },
                  onChanged: (final value) {
                    setState(() {
                      _tempFormPatient.guardianPhoneNumber = value?.trim();
                    });
                  },
                ),
                const SizedBox(height: 20),

                // Submit and Cancel Buttons
                Row(
                  children: <Widget>[
                    // submit button
                    Expanded(
                      child: ElevatedButton(
                        key: const Key('submit_form'),
                        onPressed: () async {
                          if (_formKey.currentState?.saveAndValidate() ??
                              false) {
                            // initialize the patient and check if it succeeded
                            // do different actions for an addition vs. a create
                            Patient? patientSubmitted;

                            if (isUpdateForm) {
                              // Update the patient, then kick the user out
                              // check what patient I got back after the update
                              try {
                                // If in guest use local storage
                                if (_authController.isGuestLoggedIn()) {
                                  _tempFormPatient.therapistId = _authController.guestId;
                                  final bool saveSucceeded = await LocalPatientController.updatePatient(_tempFormPatient);
                                  patientSubmitted = (await LocalPatientController.getPatientByID(_tempFormPatient.id!))!;
                                }
                                // otherwise save to database
                                else {
                                  final res =
                                  await PatientController.modifyPatientByID(
                                      _tempFormPatient);
                                  final resBody = jsonDecode(res.body);
                                  patientSubmitted =
                                  await PatientController.getPatientByID(
                                      resBody['id']);
                                }

                                ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                                    content: Text(
                                        'Patient ${patientSubmitted.fName} ${patientSubmitted.lName} updated successfully!')));
                                Navigator.pushReplacement(
                                  context,
                                  MaterialPageRoute(
                                    // this is the new patient addition, no passed in param
                                    builder: (final context) =>
                                        const PatientList(),
                                  ),
                                );
                              } catch (e) {
                                showDialog(
                                  context: context,
                                  builder: (final context) => AlertDialog(
                                    title: const Text('Error'),
                                    content: const Text(
                                        'Error modifying the patient. Please try again later.'),
                                    actions: [
                                      TextButton(
                                        onPressed: () {
                                          Navigator.of(context)
                                              .pop(); // Close the dialog
                                        },
                                        child: const Text('OK'),
                                      ),
                                    ],
                                  ),
                                );

                                debugPrint(e.toString());
                                debugPrint(
                                    patientSubmitted?.toJson().toString());
                                debugPrint(
                                    _tempFormPatient.toJson().toString());
                              }
                            } else {
                              try {
                                // Set the therapistId of the patient to the currently
                                // logged in therapist.
                                final authController =
                                    Provider.of<AuthController>(context,
                                        listen: false);
                                _tempFormPatient.therapistId =
                                    authController.therapistId ?? '';

                                // If in guest mode fetch from local storage
                                if (authController.isGuestLoggedIn()) {
                                  print("Saving patient locally");
                                  _tempFormPatient.therapistId = authController.guestId;
                                  final String patientId = await LocalPatientController.savePatient(_tempFormPatient);
                                  patientSubmitted = await LocalPatientController.getPatientByID(patientId);
                                }
                                else {
                                  // Submit the patient to the db
                                  // then retrieve it and its ID
                                  final res = await PatientController
                                      .postPatientToDatabase(_tempFormPatient);

                                  final resBodyJSON = json.decode(res.body);
                                  patientSubmitted =
                                  await PatientController.getPatientByID(
                                      resBodyJSON['patientId']);
                                }


                              } catch (e) {
                                debugPrint(e.toString());
                                debugPrint(
                                    patientSubmitted?.toJson().toString());
                                debugPrint(
                                    _tempFormPatient.toJson().toString());
                              }
                              // Show success or failure dialog
                              showDialog(
                                context: context,
                                builder: (final BuildContext context) {
                                  return AlertDialog(
                                    key: const Key('submission_result'),
                                    title: Text(patientSubmitted != null
                                        ? 'Success'
                                        : 'Error'),
                                    content: Text(patientSubmitted != null
                                        ? 'Patient ${patientSubmitted.fName} ${patientSubmitted.lName}'
                                            ' was successfully registered with an ID of ${patientSubmitted.id}'
                                        : 'Failed to submit patient.'),
                                    actions: <Widget>[
                                      TextButton(
                                        onPressed: () {
                                          Navigator.of(context).pop();
                                          // if the patient was successful, I would
                                          // want to clear the field and reset
                                          // the patient
                                          if (patientSubmitted != null &&
                                              !isUpdateForm) {
                                            _formKey.currentState?.reset();
                                            _tempFormPatient = Patient(
                                              fName: '',
                                              lName: '',
                                              condition: '',
                                              phone: '',
                                              age: 0,
                                              email: '',
                                              doctorPhoneNumber: '',
                                              weight: null,
                                              height: null,
                                              guardianPhoneNumber: null,
                                              archivalDate: null,
                                            );
                                          }
                                        },
                                        child: const Text('OK'),
                                      ),
                                    ],
                                  );
                                },
                              );
                            }
                          } else {
                            showDialog(
                              context: context,
                              builder: (final BuildContext context) {
                                return AlertDialog(
                                  title: const Text('Form has detected errors'),
                                  content: const Text(
                                      'Please fix the errors and try again.'),
                                  actions: <Widget>[
                                    TextButton(
                                      onPressed: () {
                                        Navigator.of(context).pop();
                                      },
                                      child: const Text('OK'),
                                    ),
                                  ],
                                );
                              },
                            );
                          }
                        },
                        child: Text(isUpdateForm
                            ? 'Update Patient'
                            : 'Add New Patient'),
                      ),
                    ),
                    const SizedBox(width: 20),

                    // Cancel button
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () {
                          // Navigate back to the list when canceling the edit
                          Navigator.pushReplacement(
                              context,
                              MaterialPageRoute(
                                  builder: (final context) =>
                                      const PatientList()));
                        },
                        child: const Text('Cancel'),
                      ),
                    ),
                  ],
                )
              ],
            ),
          ),
        ),
      ),
    );
  }
}
