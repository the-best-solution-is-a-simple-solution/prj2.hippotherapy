import 'package:flutter/material.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/pages/archived_patients_page.dart';
import 'package:frontend/pages/patient_info_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:frontend/widgets/generate_list_card_widget.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';

class TutorialPage extends StatelessWidget {
  static const String RouteName = '/tutorials';

  const TutorialPage({super.key});

  // Starts a tutorial for a given page based on user selection.
  // Navigates to the appropriate page with showTutorial flag set to
  // true which initiates the tutorial
  void _startTutorial(final BuildContext context, final String tutorialName) {
    switch (tutorialName) {
      case 'Patient List Tutorial':
        Navigator.pushNamed(
          context,
          PatientList.RouteName,
          arguments: {'showTutorial': true},
        );
        break;
      case 'Patient Sessions':
        Navigator.pushNamed(
          context,
          PatientInfoPage.RouteName,
          arguments: {
            'patient': Patient.placeholderPatient(),
            'showTutorial': true,
          },
        );
        break;
      case 'Patient Archive':
        Navigator.pushNamed(
          context,
          ArchivedPatientsListPage.RouteName,
          arguments: {'showTutorial': true},
        );
        break;
      case 'Patient Reassignment':
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (final context) => GenerateListCardWidget(
              objects: [Patient.placeholderPatient()],
              roleCheck: false,
              patientMoveBar: true,
              title: 'Patients under Sample Therapist',
              therapistOld: Therapist.placeholderTherapist(),
              showTutorial: true,
            ),
          ),
        );
        break;
      default:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Tutorial not implemented yet')),
        );
    }
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text("Select a Tutorial"),
      ),
      drawer: const HippoAppDrawer(),
      body: ListView.separated(
        padding: const EdgeInsets.all(16.0),
        itemCount: 4,
        separatorBuilder: (final context, final index) => const Divider(height: 1.0),
        itemBuilder: (final context, final index) {
          final tutorials = [
            {"title": "Creating, Updating, and Deleting Patients", "icon": Icons.person_add, "action": "Patient List Tutorial"},
            {"title": "Creating Patient Sessions", "icon": Icons.edit_note, "action": "Patient Sessions"},
            {"title": "Patient Archive", "icon": Icons.archive, "action": "Patient Archive"},
            {"title": "Reassigning Patients", "icon": Icons.person_pin, "action": "Patient Reassignment"},
          ];
          final tutorial = tutorials[index];
          return Card(
            margin: const EdgeInsets.symmetric(vertical: 8.0),
            elevation: 2.0,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.0)),
            child: ListTile(
              leading: Icon(tutorial["icon"] as IconData, color: Colors.blueAccent),
              title: Text(
                tutorial["title"] as String,
              ),
              trailing: const Icon(Icons.arrow_forward_ios, size: 16.0),
              onTap: () => _startTutorial(context, tutorial["action"] as String),
            ),
          );
        },
      ),
    );
  }
}