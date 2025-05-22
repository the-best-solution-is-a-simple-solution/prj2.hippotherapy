import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/pages/archive_page.dart';
import 'package:frontend/pages/archived_patients_page.dart';
import 'package:frontend/pages/export_page.dart';
import 'package:frontend/pages/generate_referral.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/pages/owner_registration_page.dart';
import 'package:frontend/pages/password_reset_page.dart';
import 'package:frontend/pages/patient_info_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:frontend/pages/registration_page.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:frontend/pages/therapist_profile_page.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/auth_widgets/protected_route.dart';
import 'package:provider/provider.dart';

void main() async {
  // Render web elements for testing with Cypress
  WidgetsFlutterBinding.ensureInitialized();
  SemanticsBinding.instance.ensureSemantics();

  runApp(
    ChangeNotifierProvider(
      // Wrap with ChangeNotifierProvider for state management.
      // Allows any widget in the tree to access AuthController.
      create: (final _) {
        final authController = AuthController();
        authController.initialize();
        return authController;
      },
      child: const Hippotherapy(),
    ),
  );
}

// Core widget which starts the app
class Hippotherapy extends StatelessWidget {
  const Hippotherapy({super.key});

  static final Map<String, WidgetBuilder> routes = {
    LoginPage.RouteName: (final context) => ProtectedRoute(
          reverse: true, // Only allow unauthenticated users
          child: const LoginPage(
              title: 'Hippotherapy', key: Key('therapist_login')),
          builder: (final context) => const LoginPage(
              title: 'Hippotherapy', key: Key('therapist_login')),
        ),
    RegistrationPage.RouteName: (final context) => ProtectedRoute(
          reverse: true, // Only allow unauthenticated users
          child: const RegistrationPage(),
          builder: (final context) => const RegistrationPage(),
        ),
    ProfilePage.RouteName: (final context) => ProtectedRoute(
          child: const ProfilePage(),
          builder: (final context) => const ProfilePage(),
        ),
    PatientList.RouteName: (final context) {
      final args =
          ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
      final showTutorial = args?['showTutorial'] == true; // Defaults to false
      return PatientList(showTutorial: showTutorial);
    },
    TherapistListPage.RouteName: (final context) => ProtectedRoute(
      child: const TherapistListPage(),
      builder: (final context) => const TherapistListPage(),
    ),
    OwnerRegistrationPage.RouteName: (final context) => ProtectedRoute(
          reverse: true, // Only allow unauthenticated users
          child: const OwnerRegistrationPage(key: Key('owner_registration')),
          builder: (final context) =>
              const OwnerRegistrationPage(key: Key('owner_registration')),
        ),
    PasswordResetPage.RouteName: (final context) => ProtectedRoute(
          reverse: true, // Only allow unauthenticated users
          child: const PasswordResetPage(),
          builder: (final context) => const PasswordResetPage(),
        ),
    ExportPage.RouteName: (final context) => ProtectedRoute(
          child: const ExportPage(),
          builder: (final context) => const ExportPage(),
        ),
    ArchivePage.RouteName: (final context) => ProtectedRoute(
          child: const ArchivePage(),
          builder: (final context) => const ArchivePage(),
        ),
    GenerateReferralPage.routeName: (final context) => ProtectedRoute(
          child: const GenerateReferralPage(),
          builder: (final context) => const GenerateReferralPage(),
        ),
    ArchivedPatientsListPage.RouteName: (final context) {
      final args =
      ModalRoute
          .of(context)!
          .settings
          .arguments as Map<String, dynamic>?;
      final showTutorial = args?['showTutorial'] == true;
      return ArchivedPatientsListPage(showTutorial: showTutorial);
    },
    TutorialPage.RouteName: (final context) => ProtectedRoute(
          child: const TutorialPage(),
          builder: (final context) => const TutorialPage(),
        ),
    PatientInfoPage.RouteName: (final context) {
      final args =
          ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>;
      final patient = args['patient'] as Patient;
      final showTutorial =
          args['showTutorial'] as bool? ?? false; // Fallback to false
      return PatientInfoPage(
        patient: patient,
        showTutorial: showTutorial, // Pass to constructor
      );
    },
  };

  @override
  Widget build(final BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Hippotherapy',
      theme: ThemeData(
        tabBarTheme: const TabBarTheme(
            labelColor: Colors.black,
            unselectedLabelColor: Colors.black,
            indicatorColor: Colors.black),
        colorScheme: ColorScheme.fromSeed(seedColor: const Color.fromRGBO(134, 51, 34, 100), primary: const Color.fromRGBO(134, 51, 34, 100), secondary: const Color.fromRGBO(175, 66, 44, 100)),
        useMaterial3: true,
      ),
      initialRoute: LoginPage.RouteName,
      routes: routes,
    );
  }
}
