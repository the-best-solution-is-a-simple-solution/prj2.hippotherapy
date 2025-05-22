import 'package:azlistview/azlistview.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/archive_controller.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/archive_widgets/patient_archive_widget.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:provider/provider.dart';
import 'package:tutorial_coach_mark/tutorial_coach_mark.dart';

class ArchivedPatientsListPage extends StatefulWidget {
  static const String RouteName = '/archived-patients';
  final bool showTutorial;
  const ArchivedPatientsListPage({super.key, this.showTutorial = false});

  @override
  State<ArchivedPatientsListPage> createState() => _ArchivedPatientListState();
}

class _ArchivedPatientListState extends State<ArchivedPatientsListPage> {
  List<Patient> patientList = [];
  bool keepLoading = true;
  final ArchiveController archiveController = ArchiveController();
  final GlobalKey _restoreButtonKey = GlobalKey();
  final GlobalKey _deleteButtonKey = GlobalKey();
  bool isTutorialActive = false;
  late TutorialCoachMark tutorialCoachMark;

  @override
  void initState() {
    super.initState();
    initList();
  }

  // Fetches the list of archived patients for the current therapist and initializes the UI state.
  Future<void> initList() async {
    setState(() {
      keepLoading = true;
    });

    if (widget.showTutorial) {
      // Tutorial mode: use placeholder data
      setState(() {
        isTutorialActive = true;
        patientList = [Patient.placeholderPatient()]; // Placeholder patient
        keepLoading = false;
      });
      WidgetsBinding.instance.addPostFrameCallback((final _) {
        if (mounted) {
          showPatientArchiveTutorial();
        }
      });
    } else {
      // Normal mode: fetch real archived patients
      final authController = Provider.of<AuthController>(context, listen: false);
      final therapistId = authController.therapistId ?? '';
      if (therapistId.isEmpty) {
        if (mounted) {
          setState(() {
            keepLoading = false;
          });
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Therapist ID not found. Please log in again.')),
          );
        }
        return;
      }

      try {
        final archivedPatients = await archiveController.getArchivedPatientList(therapistId);
        if (mounted) {
          setState(() {
            patientList = archivedPatients;
            _handleList(patientList);
            keepLoading = false;
          });
        }
      } catch (e) {
        if (mounted) {
          setState(() {
            keepLoading = false;
            patientList = [];
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error loading archived patients: $e')),
          );
        }
      }
    }
  }

  // Processes the patient list to assign suspension tags and sort it alphabetically for display.
  void _handleList(final List<Patient> list) {
    if (list.isEmpty) {
      return;
    }
    for (var i = 0; i < list.length; i++) {
      final patient = list[i];
      String tag = patient.fName.isNotEmpty == true
          ? patient.fName[0].toUpperCase()
          : '#';
      if (!RegExp(r'^[A-Z]$').hasMatch(tag)) {
        tag = '#';
      }
      patient.tag = tag;
      patient.isShowSuspension = i == 0 || patient.tag != list[i - 1].tag;
    }
    SuspensionUtil.sortListBySuspensionTag(list);
  }

  // Refreshes the archived patient list by re-fetching data from the server.
  Future<void> refreshPatientList() async {
    await initList();
  }

  // Restores an archived patient to active status and updates the UI.
  Future<void> restorePatient(final ISuspensionBean identity) async {
    final Patient patient = identity as Patient;
    try {
      final response = await archiveController.restorePatient(patient.id!);
      if (response.statusCode == 200) {
        if (mounted) {
          setState(() {
            patientList.removeWhere((final p) => p.id == patient.id);
            _handleList(patientList); // Reprocess list for headers
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content:
                  Text('${patient.fName} ${patient.lName} has been restored.'),
              action: SnackBarAction(
                label: 'View Active',
                onPressed: () => Navigator.pop(context),
              ),
            ),
          );
        }
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Failed to restore the patient.')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error restoring patient: $e')),
        );
      }
    }
  }

  // Permanently deletes an archived patient after user confirmation and updates the UI.
  Future<void> deletePatient(final ISuspensionBean identity) async {
    final Patient patient = identity as Patient;
    final bool? confirm = await showDialog<bool>(
      context: context,
      builder: (final context) => AlertDialog(
        title: const Text('Confirm Permanent Deletion'),
        content: Text(
          'Are you sure you want to permanently delete ${patient.fName} ${patient.lName}? '
          'This will anonymize their sessions/evaluations and cannot be undone.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirm != true) {
      return;
    }

    try {
      final response = await archiveController.deletePatient(patient.id!);
      if (response.statusCode == 204) {
        if (mounted) {
          setState(() {
            patientList.removeWhere((final p) => p.id == patient.id);
            _handleList(patientList); // Reprocess list for headers
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
                content: Text(
                    '${patient.fName} ${patient.lName} has been permanently deleted.')),
          );
        }
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Failed to delete the patient.')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error deleting patient: $e')),
        );
      }
    }
  }

  // Builds a header widget for each alphabetical section in the patient list.
  Widget _buildHeader(final String tag) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 12),
      child: Text(
        tag,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
      ),
    );
  }

  // Displays the tutorial for the archive page
  void showPatientArchiveTutorial() {
    if (patientList.isEmpty) {
      return;
    }
    final List<TargetFocus> targets = [
      TargetFocus(
        identify: "restore_button",
        keyTarget: _restoreButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
                  "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
      TargetFocus(
        identify: "delete_button",
        keyTarget: _deleteButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "To permanently delete a patient, "
                  "press the delete button. This will permanently delete the "
                  "patient and cannot be undone.",
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
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text('Archived Patients'),
        elevation: 0,
      ),
      drawer: const HippoAppDrawer(),
      body: RefreshIndicator(
        onRefresh: refreshPatientList,
        child: keepLoading
            ? const Center(child: CircularProgressIndicator())
            : patientList.isEmpty
            ? const Center(
          key: Key('no_archived_patients'),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SizedBox(height: 16),
              Text('No archived patients found.'),
            ],
          ),
        )
            : AzListView(
          key: const Key('archived_patient_list'),
          data: patientList,
          itemCount: patientList.length,
          itemBuilder: (final context, final index) {
            final patient = patientList[index];
            final bool isFirstPatient = index == 0;
            return patient.isShowSuspension == true
                ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _buildHeader(patient.tag ?? '#'),
                PatientArchiveWidget(
                  key: Key('archived_patient_${patient.id}'),
                  patient: patient,
                  onRestoreClick: restorePatient,
                  onDeleteClick: deletePatient,
                  restoreButtonKey: isFirstPatient ? _restoreButtonKey : null,
                  deleteButtonKey: isFirstPatient ? _deleteButtonKey : null,
                ),
              ],
            )
                : PatientArchiveWidget(
              key: Key('archived_patient_${patient.id}'),
              patient: patient,
              onRestoreClick: restorePatient,
              onDeleteClick: deletePatient,
              restoreButtonKey: isFirstPatient ? _restoreButtonKey : null,
              deleteButtonKey: isFirstPatient ? _deleteButtonKey : null,
            );
          },
          padding: const EdgeInsets.all(12.0),
          indexBarOptions: IndexBarOptions(
            needRebuild: true,
            indexHintAlignment: Alignment.centerRight,
            hapticFeedback: true,
            textStyle: Theme.of(context).textTheme.bodyMedium!.copyWith(
              fontWeight: FontWeight.bold,
            ),
            selectItemDecoration: const BoxDecoration(
              shape: BoxShape.circle,
            ),
          ),
        ),
      ),
    );
  }
}