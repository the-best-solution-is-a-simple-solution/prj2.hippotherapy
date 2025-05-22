import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/export_page.dart';
import 'package:frontend/pages/generate_referral.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:frontend/pages/registration_page.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:frontend/pages/therapist_profile_page.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:provider/provider.dart';

class HippoAppDrawer extends StatelessWidget implements PreferredSizeWidget {
  const HippoAppDrawer({super.key});

  List<Widget> allowedTabs(final BuildContext context) {
    final List<Widget> allowedTabs = [
      ListTile(
        key: const Key('logout_btn'),
        leading: const Icon(Icons.exit_to_app, color: Colors.red),
        title: const Text('Logout',
            style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
        onTap: () {
          Provider.of<AuthController>(context, listen: false)
              .logout()
              .then((final _) {
            if (context.mounted) {
              Navigator.pushReplacementNamed(context, LoginPage.RouteName);
            }
          });
        },
      ),
    ];

    if (Provider.of<AuthController>(context, listen: false).ownerId != null) {
      //  checking if role of login is "owner".
      return <Widget>[
            ListTile(
                key: const Key('therapist_list_main'),
                title: const Text('Therapists'),
                onTap: () {
                  Navigator.pushNamed(context, TherapistListPage.RouteName);
                }),
            ListTile(
                key: const Key("referral_page"),
                title: const Text("Referral"),
                onTap: () {
                  Navigator.pushReplacementNamed(
                      context, GenerateReferralPage.routeName);
                }),
            ListTile(
              title: const Text('Export'),
              onTap: () {
                Navigator.pushReplacementNamed(context, ExportPage.RouteName);
              },
            ),
        ListTile(
          key: const Key('tutorials_tile'),
          title: const Text('Tutorials'),
          onTap: () {
            Navigator.pushNamed(context, TutorialPage.RouteName);
          },
        ),
          ] +
          allowedTabs;
    } 
    // In guest mode only show patients list page as route
    else if (AuthController().isGuestLoggedIn()) {
      // if not owner, then it is therapist
      return <Widget>[
            ListTile(
              key: const Key('patient_list_main'),
              title: const Text('Patients'),
              onTap: () {
                Navigator.pushNamed(context, PatientList.RouteName);
              },
            ),
          ] +
          allowedTabs;
    }
    else {
      // if not owner, then it is therapist
      return <Widget>[
        ListTile(
          title: const Text('Profile'),
          onTap: () {
            Navigator.pushNamed(context, ProfilePage.RouteName);
          },
        ),
        ListTile(
          key: const Key('patient_list_main'),
          title: const Text('Patients'),
          onTap: () {
            Navigator.pushNamed(context, PatientList.RouteName);
          },
        ),
        ListTile(
          title: const Text('Archive'),
          onTap: () {
            Navigator.pushNamed(
                context, '/archive'); // Navigate to ArchivePage
          },
        ),
        ListTile(
          key: const Key('tutorials_tile'),
          title: const Text('Tutorials'),
          onTap: () {
            Navigator.pushNamed(context, TutorialPage.RouteName);
          },
        ),
      ] +
          allowedTabs;
    }
  }

  @override
  Widget build(final BuildContext context) {
    return Drawer(
      // Add a ListView to the drawer. This ensures the user can scroll
      // through the options in the drawer if there isn't enough vertical
      // space to fit everything.

      child: ListView(
        // Important: Remove any padding from the ListView.
        padding: EdgeInsets.zero,
        children: [
          DrawerHeader(
            decoration: BoxDecoration(color: Theme.of(context).colorScheme.primary),
            child: Text('Menu'),
          ),
          // Conditional to display different links depending on login status
          FutureBuilder<bool>(
            future: Provider.of<AuthController>(context, listen: false)
                .checkLoginStatus(),
            builder: (final context, final snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return Container();
              } else if (snapshot.hasData) {
                final isLoggedIn =
                    Provider.of<AuthController>(context, listen: false)
                        .isLoggedIn;
                // logged in
                if (isLoggedIn) {
                  return Column(
                    children: allowedTabs(context),
                  );
                } else {
                  // not logged in
                  return Column(
                    children: [
                      ListTile(
                        title: const Text('Register'),
                        key: const Key('register_tile'),
                        onTap: () {
                          Navigator.pushReplacementNamed(
                              context, RegistrationPage.RouteName);
                        },
                      ),
                      ListTile(
                        title: const Text('Login'),
                        onTap: () {
                          Navigator.pushReplacementNamed(
                              context, LoginPage.RouteName);
                        },
                      ),
                    ],
                  );
                }
              } else {
                return Container();
              }
            },
          ),
        ],
      ),
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight * 2);
}
