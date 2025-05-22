// lib/config.dart or lib/constants/server_config.dart
/// This uses the philosophy that there will be less errors if we assemble the
/// route in only one place (so we don't have to worry about if stuff needs a
/// slash before it or not)
class ServerConfig {
  // TODO: change these to real values when deploying for production
  //region Variables
  static const String address = 'http://localhost';
  static const String port = '5001';

  // TODO: keep for now but do not use, is replaced by stuff below
  static const String integrationTestsRoute = 'integration-tests';
  static const String patientRoute = '/patient';
  static const String patientEvaluationRoute = '/patientevaluation';
  static const String patientEvaluationPostRoute = '/submit-evaluation';
  static const String sessionRoute = '/session';
  static const String sessionPostRoute = '/submit-session';
  static const String exportRoute = '/export';
  static const String patientPostRoute = '/submit-patient';
  static const String ownerRoute = '/owners';
  static const String therapistRoute = '/therapists';
  static const String archiveRoute = '/archive';
  static const String archiveRestoreRoute = '/archive/restore/';

  // New dynamic way to get backend routes
  // Pass in any required parts of the route
  static const String _baseAddress = "$address:$port";

  static const String patientBaseRoute = "patient";
  static const String patientEvaluationBaseRoute = "patientevaluation";
  static const String ownerBaseRoute = "owners";
  static const String therapistBaseRoute = "therapists";
  static const String integrationTestsBaseRoute = "integration-tests";
  static const String exportBaseRoute = "export";
  static const String sessionBaseRoute = "session";
  static const String authBaseRoute = "auth";
  static const String archiveBaseRoute = "archive";
  static const String cacheClear = 'cache-clear';
  static const String cache = 'cache';
  static const String logBaseRoute = "log";

  // Routes that do not take parameters
  static const String uniqueNamesRoute = "get-unique-names";
  static const String uniqueConditionsRoute = "get-unique-conditions";

  //endregion

  //region Owner Controller Routes

  /// Gets the full route including the base address and the port.
  /// E.g. http://localhost:5001/owners/{ownerId}
  static String getTherapistsByOwnerIdRoute(final String ownerId) {
    return "$_baseAddress/$ownerBaseRoute/$ownerId/therapists";
  }

  /// Gets the full route including the base address and the port.
  static String getOwnerByIdRoute(final String ownerId) {
    return "$_baseAddress/$ownerBaseRoute/$ownerId";
  }

  /// Gets the full route including the base address and the port.
  static String getReassignPatientsToDifferentTherapistRoute(
      final String ownerId,
      final String oldTherapistId,
      final String newTherapistId) {
    return "$_baseAddress/$ownerBaseRoute/$ownerId/$oldTherapistId/$newTherapistId";
  }

  //endregion

  //region Patient Controller Routes

  static String getCreatePatientRoute(final String therapistId) {
    return "$_baseAddress/$patientBaseRoute/submit-patient/$therapistId";
  }

  static String getPatientListByTherapistIdRoute(final String therapistId) {
    return "$_baseAddress/$patientBaseRoute/therapist/$therapistId";
  }

  static String getPatientByIdRoute(final String patientId) {
    return "$_baseAddress/$patientBaseRoute/$patientId";
  }

  static String getUpdatePatientRoute(final String patientId) {
    return "$_baseAddress/$patientBaseRoute/$patientId";
  }

  static String getArchivePatientRoute(final String patientId) {
    return "$_baseAddress/$patientBaseRoute/$patientId";
  }

  //endregion

  //region Archive Controller Routes
  /// Gets the full route including the base address and the port.
  static String getRestorePatientRoute(final String patientId) {
    return "$_baseAddress/$archiveBaseRoute/restore/$patientId";
  }

  static String getDeletePatientRoute(final String patientId) {
    return "$_baseAddress/$archiveBaseRoute/$patientId";
  }

  static String getArchivedPatientListByTherapistIdRoute(
      final String therapistId) {
    return "$_baseAddress/$archiveBaseRoute/therapist/$therapistId";
  }

  //endregion

  //region AuthController Routes
  /// Gets the full route including the base address and the port.
  static String getRequestPasswordResetEmailRoute() {
    return "$_baseAddress/$authBaseRoute/request-password-reset-email";
  }

  static String getRegisterTherapistRoute() {
    return "$_baseAddress/$authBaseRoute/therapist/register";
  }

  static String getLoginTherapistRoute() {
    return "$_baseAddress/$authBaseRoute/therapist/login";
  }

  static String getRegisterOwnerRoute() {
    return "$_baseAddress/$authBaseRoute/owner/register";
  }

  static String getLoginOwnerRoute() {
    return "$_baseAddress/$authBaseRoute/owner/login";
  }

  static String getGenerateReferralRoute() {
    return "$_baseAddress/$authBaseRoute/referral";
  }

  //endregion

  //region Export Controller Routes
  static String getRecordsRoute(final String ownerId) {
    return "$_baseAddress/$exportBaseRoute/records/$ownerId";
  }

  static String getUniqueLocationsRoute(final String ownerId) {
    return "$_baseAddress/$exportBaseRoute/get-unique-locations/$ownerId";
  }

  static String getLowestYearRoute(final String ownerId) {
    return "$_baseAddress/$exportBaseRoute/get-lowest-year/$ownerId";
  }

  static String getUniqueNamesRoute(final String ownerId) {
    return "$_baseAddress/$exportBaseRoute/get-unique-names/$ownerId";
  }

  static String getUniqueConditionsRoute(final String ownerId) {
    return "$_baseAddress/$exportBaseRoute/get-unique-conditions/$ownerId";
  }

  //endregion

  //region Patient Evaluation Controller Routes
  static String getExistingCachedRoute(
      final String patientID, final String sessionID, final String evalType) {
    return '$_baseAddress/$patientEvaluationBaseRoute/$patientID/$cache/$sessionID/$evalType';
  }

  static String getSaveCacheRoute(
      final String patientID, final String sessionID) {
    return '$_baseAddress/$patientEvaluationBaseRoute/$patientID/$cache/$sessionID';
  }

  static String getClearSavedCacheRoute(
      final String patientID, final String sessionID, final String evalType) {
    return '$_baseAddress/$patientEvaluationBaseRoute/$patientID/$cacheClear/$sessionID/$evalType';
  }

  static String getEvaluationByIdRoute(final String evaluationUUID) {
    return "$_baseAddress/$patientEvaluationBaseRoute/$evaluationUUID";
  }

  static String getCreatePatientEvaluationRoute(final String patientId) {
    return "$_baseAddress/$patientEvaluationBaseRoute/$patientId/submit-evaluation";
  }

  static String getAllEvaluationDataForGraphRoute(final String patientId) {
    return "$_baseAddress/$patientEvaluationBaseRoute/patient/$patientId";
  }

  static String getEvaluationTagListRoute() {
    return "$_baseAddress/$authBaseRoute/tags";
  }

  //endregion

  //region Session Controller Routes
  static String getCreateSessionRoute(final String patientId) {
    return "$_baseAddress/$sessionBaseRoute/patient/$patientId/submit-session";
  }

  static String getAllSessionsRoute(final String patientId) {
    return "$_baseAddress/$sessionBaseRoute/patient/$patientId/session";
  }

  static String getPrePostEvaluationsRoute(
      final String patientId, final String sessionId) {
    return "$_baseAddress/$sessionBaseRoute/patient/$patientId/session/$sessionId/pre-post";
  }

  //endregion

// --------------------Therapist Controller Routes--------------------//
  static String getTherapistByIdRoute(final String therapistId) {
    return "$_baseAddress/$therapistBaseRoute/$therapistId";
  }

// --------------------[other] Controller Routes--------------------//

  //region Integration Seed Data Test Routes

  static String getSeedOwnerTherapistInfoRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-owner-therapist-info";
  }

  static String getSeedPatientInfoSessionTabRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-patient-info-session-tab";
  }

  static String getSeedPatientInfoGraphTabRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-patient-info-graph-tab";
  }

  static String getSeedEvaluationPageData() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-evaluation-page-data";
  }

  static String getSeedExportPageData() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-patient-export-page-data";
  }

  static String getSeedPatientListPageRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-patient-list-page-data";
  }

  static String getClearEmulatorAuthRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/clear-auth";
  }

  static String getClearEmulatorDataRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/clear";
  }

  static String getSeedArchiveDataRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-archive-data";
  }

  static String getSeedTransferPatientDataRoute() {
    return "$_baseAddress/$integrationTestsBaseRoute/seed-transfer-patient-data";
  }

  //endregion

  //region Log Controller Routes

  /// A route to log a login for guest
  /// If not valid will not log and return bad request
  static String getLogGuestLoginRoute() {
    return "$_baseAddress/$logBaseRoute/login-guest";
  }

  //endregion
}
