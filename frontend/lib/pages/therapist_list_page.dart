import 'dart:async';
import 'dart:io';

import 'package:azlistview/azlistview.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/owner_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';
import 'package:frontend/models/owner.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/pages/patient_info_page.dart';
import 'package:frontend/widgets/generate_list_card_widget.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:provider/provider.dart';

class TherapistListPage extends StatefulWidget {
  static const String RouteName = '/therapists';
  const TherapistListPage(
      {super.key,
        this.therapistThatWillHavePatientsMoved,
        this.therapistAssignmentTitle,
        this.lstPatientsReassignment});

  static const String title = 'Therapist List';
  final Therapist? therapistThatWillHavePatientsMoved;
  final String? therapistAssignmentTitle;
  final List<Patient>? lstPatientsReassignment;

  @override
  _TherapistListState createState() => _TherapistListState();
}

class _TherapistListState extends State<TherapistListPage> {
  Owner? owner;
  bool isLoading = true;
  bool hasError = false;
  List<Therapist> therapists = [];

  // This will allow access to the patient session view when accessing patients from owner
  void cardClick(final ISuspensionBean identity) {
    Navigator.push(
        context,
        MaterialPageRoute(
            builder: (final preContext) =>
                PatientInfoPage(patient: identity as Patient)));
  }

  /// If the filter is passed in, we will know that
  /// the page is going to be used for a patient assignment
  void reassignPatients(final ISuspensionBean t) async {
    final Therapist nuTherapist = t as Therapist;
    bool wantsToAssign = false;

    if (mounted) {
      wantsToAssign = await showDialog<bool>(
            context: context,
            builder: (final BuildContext context) {
              return AlertDialog(
                title: const Text('Reassignment Confirmation'),
                content: Text(
                  'Are you sure you\'d like to move ${widget.lstPatientsReassignment!.length} '
                  'patient${widget.lstPatientsReassignment!.length == 1 ? '' : 's'} from '
                  '${widget.therapistThatWillHavePatientsMoved!.fName} '
                  '${widget.therapistThatWillHavePatientsMoved!.lName} to '
                  '${nuTherapist.fName} ${nuTherapist.lName}?',
                ),
                actions: <Widget>[
                  TextButton(
                    onPressed: () {
                      // User cancels
                      Navigator.of(context).pop(false);
                    },
                    child: const Text('Cancel'),
                  ),
                  TextButton(
                    key: const Key("reassign_patients"),
                    onPressed: () {
                      Navigator.of(context).pop(true); // user confirms
                    },
                    child: const Text('Yes'),
                  ),
                ],
              );
            },
          ) ??
          false;
    }

    if (wantsToAssign == true) {
      final List<String> lstPIDsToReassign =
          widget.lstPatientsReassignment!.map((final x) => x.id!).toList();

      try {
        final res = await OwnerController.reassignPatientsToDiffTherapist(
            owner!.ownerId!,
            widget.therapistThatWillHavePatientsMoved!.therapistID!,
            nuTherapist.therapistID!,
            lstPIDsToReassign);

        debugPrint(res.toString());

        if (mounted) {
          await showDialog<bool>(
            context: context,
            builder: (final BuildContext context) {
              return AlertDialog(
                key: const Key('patient_assignment_notification_result'),
                title: Text(res.statusCode == HttpStatus.ok
                    ? 'Success Moving Patients'
                    : 'Error'),
                content: Text(
                  res.statusCode == HttpStatus.ok
                      ? 'The operation was successful, you may now refresh the page.'
                      : 'There was an error moving the patients.\n'
                          '${res.body.toString()}',
                ),
                actions: <Widget>[
                  TextButton(
                    key: const Key("patient_moved_ack"),
                    onPressed: () {
                      Navigator.pushReplacement(
                          context,
                          MaterialPageRoute(
                              builder: (final context) =>
                                  const TherapistListPage()));
                    },
                    child: const Text('Okay'),
                  ),
                ],
              );
            },
          );
        }
      } catch (e) {
        debugPrint(e.toString());
        mounted
            ? await showDialog<bool>(
                context: context,
                builder: (final BuildContext context) {
                  return AlertDialog(
                    key: const Key('patient_assignment_notification_error'),
                    title: const Text('There was an error'),
                    content: const Text(
                      'Please try again later, refresh the page to continue',
                    ),
                    actions: <Widget>[
                      TextButton(
                        key: const Key("patient_err_ack"),
                        onPressed: () {
                          Navigator.pushReplacement(
                              context,
                              MaterialPageRoute(
                                  builder: (final context) =>
                                      const TherapistListPage()));
                        },
                        child: const Text('Okay'),
                      ),
                    ],
                  );
                },
              )
            : null;
      }
    }
  }

  void onTherapistCardClick(final ISuspensionBean t) async {
    final therapistParam = (t as Therapist);

    if (therapistParam.therapistID == null) {
      throw Exception("Error no therapist id for getting patient list");
    }
    final res = await PatientController.getPatientsByTherapistId(
        therapistParam.therapistID!);
    // final res = await PatientController.getPatients();
    
    res.removeWhere((final x) => x.therapistId != therapistParam.therapistID!);

    if (res.isEmpty && mounted) {
      showDialog(
        context: context,
        builder: (final BuildContext context) {
          return AlertDialog(
            title: Text('${therapistParam.fName} ${therapistParam.lName} '
                'has no patients under their care'),
            content: const Text('Please select a different therapist.'),
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
    } else {
      mounted
          ? Navigator.push(
              context,
              MaterialPageRoute(
                  builder: (final context) => GenerateListCardWidget(
                      objects: res,
                      roleCheck: false,
                      patientMoveBar: true,
                      cardClick: (final identity) => cardClick(identity),
                      title: 'Patients under ${therapistParam.fName} '
                          '${therapistParam.lName}',
                      therapistOld: t)))
          : null;
    }
  }

  Future<void> fetchTherapistList() async {
    final authController = Provider.of<AuthController>(context, listen: false);
    try {
      final String ownerId = authController.ownerId!;

      owner = await authController.getOwnerInfo(ownerId);

      // need to manually sort here to match the display widget's state
      therapists = await OwnerController.getTherapistsByOwnerId(ownerId);

      // if this page is meant for assignments, we will hide the currently passed
      // in therapist
      if (widget.therapistThatWillHavePatientsMoved != null) {
        therapists.removeWhere((final x) =>
            x.therapistID! ==
            widget.therapistThatWillHavePatientsMoved!.therapistID!);
      }
      SuspensionUtil.sortListBySuspensionTag(therapists);

      if (mounted) {
        setState(() {});
      }
    } catch (e) {
      hasError = true;
    } finally {
      Timer(const Duration(seconds: 3), () async {
        isLoading = false;
        if (mounted) {
          setState(() {});
        }
      });
    }
  }

  @override
  void initState() {
    super.initState();
    fetchTherapistList();
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
          backgroundColor: Theme.of(context).colorScheme.primary,
          title:
              Text(widget.therapistAssignmentTitle ?? TherapistListPage.title),
          actions: const []),
      drawer: const HippoAppDrawer(),
      body: therapists.isNotEmpty
          ? GenerateListCardWidget(
              objects: therapists,
              roleCheck: true,
              cardClick: widget.therapistThatWillHavePatientsMoved != null
                  ? reassignPatients
                  : onTherapistCardClick,
              patientMoveBar: false,
            )
          : isLoading
              ? const Center(child: CircularProgressIndicator())
              : const Center(
                  child: Text("No therapists found.",
                      style: TextStyle(fontSize: 20, color: Colors.red))),
    );
  }
}
