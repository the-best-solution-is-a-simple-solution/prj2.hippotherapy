import 'package:flutter/material.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/archived_patients_page.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';

class ArchivePage extends StatelessWidget {
  static const String RouteName = '/archive';
  const ArchivePage({super.key});

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text('Archive'),
      ),
      drawer: const HippoAppDrawer(), // Add the drawer
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.start,
          children: [
            const SizedBox(height: 20),
            Container(
              width: double.infinity,
              margin: const EdgeInsets.symmetric(horizontal: 16.0),
              child: ElevatedButton(
                onPressed: () {
                  Navigator.pushNamed(
                      context, ArchivedPatientsListPage.RouteName);
                },
                child: const Text('Patients'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
