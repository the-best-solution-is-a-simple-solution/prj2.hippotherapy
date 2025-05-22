import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:provider/provider.dart';

// Widget to restrict access to certain routes based on login status
class ProtectedRoute extends StatelessWidget {
  final Widget child;
  final Widget Function(BuildContext) builder;
  final bool reverse; // Add this parameter

  const ProtectedRoute({
    super.key,
    required this.child,
    required this.builder,
    this.reverse =
        false, // Default to false, meaning you need Auth to access the route
    // if true, authenticated users cannot access it, but unauthenticated ones can
  });

  @override
  Widget build(final BuildContext context) {
    final authController = Provider.of<AuthController>(context, listen: false);
    return FutureBuilder(
      future: authController.checkLoginStatus(),
      builder: (final context, final AsyncSnapshot<bool> snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Scaffold(
              body: Center(child: CircularProgressIndicator()));
        } else if (snapshot.hasError) {
          return const Scaffold(
              body: Center(child: Text('Error checking login status')));
        } else if (snapshot.hasData) {
          final bool isLoggedIn = snapshot.data!;

          // Reverse logic: Only allow unauthenticated users to access the route
          if (reverse) {
            if (!isLoggedIn) {
              return child; // Allow access to unauthenticated users
            } else {
              // Redirect logged-in users to the appropriate page
              SchedulerBinding.instance.addPostFrameCallback((final _) {
                redirectLoggedInUser(context, authController);
              });
              return builder(context);
            }
          }
          // Normal logic: Only allow authenticated users to access the route
          else {
            if (isLoggedIn) {
              return child; // Allow access to authenticated users
            } else {
              // Redirect unauthenticated users to the login page
              SchedulerBinding.instance.addPostFrameCallback((final _) {
                Navigator.of(context).pushNamedAndRemoveUntil(
                    LoginPage.RouteName, (final route) => false);
              });
              return builder(context);
            }
          }
        } else {
          return const Scaffold(
              body: Center(child: Text('Unexpected error occurred')));
        }
      },
    );
  }

  // Helper method to redirect logged-in users
  void redirectLoggedInUser(
      final BuildContext context, final AuthController authController) {
    if (authController.therapistId != null) {
      Navigator.of(context).pushNamedAndRemoveUntil(
          PatientList.RouteName, (final route) => false);
    } else if (authController.ownerId != null) {
      Navigator.of(context).pushNamedAndRemoveUntil(
          TherapistListPage.RouteName, (final route) => false);
    }
  }
}
