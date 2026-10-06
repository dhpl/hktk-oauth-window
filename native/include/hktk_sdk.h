#ifndef HKTK_SDK_H
#define HKTK_SDK_H

#include <stdint.h>

#if defined(_WIN32)
#if defined(HKTK_SDK_EXPORTS)
#define HKTK_API __declspec(dllexport)
#else
#define HKTK_API __declspec(dllimport)
#endif
#define HKTK_CALL __cdecl
#else
#define HKTK_API
#define HKTK_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define HKTK_CODE_CAPACITY 512
#define HKTK_STATE_CAPACITY 128
#define HKTK_URI_CAPACITY 1024
#define HKTK_ERROR_MESSAGE_CAPACITY 512

typedef struct HKTKClient HKTKClient;

typedef enum HKTKEnvironment {
  HKTK_ENVIRONMENT_SANDBOX = 0,
  HKTK_ENVIRONMENT_PRODUCTION = 1,
  HKTK_ENVIRONMENT_LOCAL = 2
} HKTKEnvironment;

typedef enum HKTKErrorCode {
  HKTK_OK = 0,
  HKTK_ERROR_INVALID_ARGUMENT = 1,
  HKTK_ERROR_ALREADY_RUNNING = 2,
  HKTK_ERROR_NETWORK = 3,
  HKTK_ERROR_BROWSER = 4,
  HKTK_ERROR_TIMEOUT = 5,
  HKTK_ERROR_CANCELLED = 6,
  HKTK_ERROR_INVALID_CALLBACK = 7,
  HKTK_ERROR_STATE_MISMATCH = 8,
  HKTK_ERROR_OAUTH = 9,
  HKTK_ERROR_INTERNAL = 10
} HKTKErrorCode;

typedef struct HKTKConfig {
  HKTKEnvironment environment;
  const char *client_id;
  const char *scope;
  const char *authorize_url;
  uint32_t timeout_seconds;
} HKTKConfig;

typedef struct HKTKLoginResult {
  char code[HKTK_CODE_CAPACITY];
  char state[HKTK_STATE_CAPACITY];
  char redirect_uri[HKTK_URI_CAPACITY];
} HKTKLoginResult;

typedef struct HKTKError {
  HKTKErrorCode code;
  char message[HKTK_ERROR_MESSAGE_CAPACITY];
} HKTKError;

HKTK_API const char *HKTK_CALL hktk_sdk_version(void);

HKTK_API HKTKClient *HKTK_CALL hktk_client_create(const HKTKConfig *config,
                                                      HKTKError *error);

HKTK_API void HKTK_CALL hktk_client_destroy(HKTKClient *client);

/*
 * Blocking login operation. Wrappers should call this on a worker thread.
 * The function opens the system browser, waits for the loopback callback,
 * validates state and returns the one-time authorization code.
 * Starting another login on the same client cancels and replaces an operation
 * that is still waiting for its browser callback.
 */
HKTK_API HKTKErrorCode HKTK_CALL hktk_client_login(HKTKClient *client,
                                                       HKTKLoginResult *result,
                                                       HKTKError *error);

HKTK_API void HKTK_CALL hktk_client_cancel(HKTKClient *client);

#ifdef __cplusplus
}
#endif

#endif
