import 'dart:io';

import 'package:azlistview/azlistview.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_patient_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/pages/patient_addition_form_page.dart';
import 'package:frontend/pages/patient_info_page.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/generate_list_card_widget.dart';
import 'package:frontend/widgets/guest_patient_list_card_widget.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:tutorial_coach_mark/tutorial_coach_mark.dart';

class PatientList extends StatefulWidget {
  static const String RouteName = '/patients';

  const PatientList({super.key, this.showTutorial = false});

  static const String title = 'Patient List';
  final bool showTutorial;

  @override
  State<PatientList> createState() => _PatientListState();
}

class _PatientListState extends State<PatientList> {
  List<Patient> patients = [];
  bool keepLoading = true;
  final AuthController _authController = AuthController();
  final GlobalKey _createButtonKey = GlobalKey();
  final GlobalKey _editButtonKey = GlobalKey();
  final GlobalKey _archiveButtonKey = GlobalKey();
  bool isTutorialActive = false;
  List<Patient> _tutorialPatients = [];
  late TutorialCoachMark tutorialCoachMark;

  // Initialize state of widget
  @override
  void initState() {
    super.initState();
    initList();
  }

  // initialize the list
  // done separately because we wont need to init the list again when editing.
  void initList() async {
    setState(() {
      keepLoading = true;
    });

    if (widget.showTutorial) {
      setState(() {
        isTutorialActive = true;
        _tutorialPatients = [Patient.placeholderPatient()];
        keepLoading = false;
      });
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          try {
            showPatientListTutorial();
          } catch (e) {
            debugPrint("Failed to initialize tutorial: $e");
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Failed to start tutorial')),
              );
            }
          }
        }
      });
    } else {
      try {
        // If in guest mode fetch from local storage
        if (_authController.isGuestLoggedIn()) {
          String therapistId = _authController.guestId!;
          print("getting locally for $therapistId");
          patients = await LocalPatientController.getPatientsByTherapistId(therapistId);
        } else {
          patients = await PatientController.getPatientsByTherapistId(_authController.therapistId!);
        }

        // Must put the sort here or it will mess up
        SuspensionUtil.sortListBySuspensionTag(patients); // IMPORTANT
      } catch (e) {
        patients = [];
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Failed to load patients: $e')),
          );
        }
      }
      if (mounted) {
        setState(() {
          keepLoading = false;
        });
      }
    }
  }

  // onclick of the patient list card
  void cardClick(final ISuspensionBean identity) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (final preContext) => PatientInfoPage(
          patient: identity as Patient,
          showTutorial: false,
        ),
      ),
    );
  }

  // to edit the patient
  void cardEdit(final ISuspensionBean identity) async {
    debugPrint("EDITING PATIENT");
    await Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (final context) =>
            PatientAdditionFormPage(patient: identity as Patient),
      ),
    );
  }

  // method to delete the card and show context
  void cardDelete(final ISuspensionBean identity) async {
    final Patient patient = identity as Patient;

    final deleteResponse =
    await PatientController.archivePatientByID(patient.id!);

    // Check if the deletion was successful
    if (deleteResponse.statusCode == HttpStatus.ok) {
      // Show a snackbar or dialog confirming deletion
      if (mounted) {
        patients.removeWhere((final p) => patient.id == p.id);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content:
            Text('${patient.fName} ${patient.lName} has been archived.'),
            duration: const Duration(seconds: 2),
          ),
        );
      }
    }

    // remove the patient from the list on the client-side
    if (mounted) {
      setState(() {
        patients.removeWhere(
                (final p) => p.id == patient.id); // remove from the list
      });
    } else {
      // handle failure to delete
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Failed to archive the patient.')),
        );
      }
    }
  }

  // Display tutorial for patient list page
  void showPatientListTutorial() {
    final List<TargetFocus> targets = [
      TargetFocus(
        identify: "create_button",
        keyTarget: _createButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "The Patient List displays all patients assigned to the currently "
                  "signed in therapist and allows them to create, update, "
                  "and delete patients.\n\n To create a new patient, click the "
                  "green + button.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
      TargetFocus(
        identify: "edit_button",
        keyTarget: _editButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "To update a patient’s information, click the update patient "
                  "button next to the patient’s name that you want to update.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
      TargetFocus(
        identify: "archive_button",
        keyTarget: _archiveButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "Archiving a patient will add the patient to the patient "
                  "archive which will prevent therapists from adding or "
                  "viewing a patient’s sessions.\n\nClick the archive button"
                  " to archive a patient.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
    ];
    tutorialCoachMark = TutorialCoachMark(
      targets: targets,
      colorShadow: Colors.black54,
      onFinish: () {
        debugPrint("Patient list tutorial finished");
        // Return to tutorial page
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
      },
      onSkip: () {
        debugPrint("Patient list tutorial skipped");
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
        return true;
      },
    );

    debugPrint("Starting patient list tutorial");
    tutorialCoachMark.show(context: context);
  }

  @override
  Widget build(final BuildContext context) {
    // Use tutorialPatients during tutorial, otherwise normal patients
    final displayPatients = isTutorialActive ? _tutorialPatients : patients;
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text(PatientList.title),
        actions: [
          Material(
            color: Colors.green,
            shape: const CircleBorder(),
            child: IconButton(
              key: _createButtonKey,
              icon: const Icon(Icons.add),
              onPressed: () {
                Navigator.pushReplacement(
                  context,
                  MaterialPageRoute(
                    builder: (final context) => const PatientAdditionFormPage(),
                  ),
                );
              },
              tooltip: 'Add a new patient',
              color: Colors.black,
            ),
          )
        ],
      ),
      drawer: const HippoAppDrawer(),

      // in case where patients is not empty, load the list view sub widget
      // otherwise show loading indicator

      // If statements are not supported inside widget building methods,
      // so i constructed a ternary to suit my needs.
      body: displayPatients.isNotEmpty
          ? PatientListCardWidget()
          : keepLoading
          ? const Center(child: CircularProgressIndicator())
          : const Center(
          child: Text("No patients found.",
              style: TextStyle(fontSize: 20, color: Colors.red))),
    );
  }

  /// Returns either the standard list card widget or
  /// a custom one for if in guest mode
  Widget PatientListCardWidget() {
    if (_authController.isGuestLoggedIn()) {
      return GuestListCardWidget(
        objects: isTutorialActive ? _tutorialPatients : patients,
        roleCheck: false,
        cardClick: (final identity) => cardClick(identity),
        editClick: (final identity) => cardEdit(identity),
        // No delete / archive
        // deleteClick: (final identity) => cardDelete(identity),
        patientMoveBar: false,
      );
    } else {
      return GenerateListCardWidget(
        key: ValueKey(isTutorialActive ? 'tutorial' : 'normal'),
        objects: isTutorialActive ? _tutorialPatients : patients,
        roleCheck: false,
        cardClick: (final identity) => cardClick(identity),
        editClick: (final identity) => cardEdit(identity),
        deleteClick: (final identity) => cardDelete(identity),
        patientMoveBar: false,
        // Pass keys for tutorial testing
        editButtonKey: _editButtonKey,
        archiveButtonKey: _archiveButtonKey,
      );
    }
  }
}