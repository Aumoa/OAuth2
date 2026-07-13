export const appMessages = {
  ko: {
    app: {
      common: {
        actions: {
          continue: '계속',
          back: '뒤로',
          register: '등록',
        },
        fields: {
          id: 'ID',
          password: '암호',
          passwordConfirmation: '암호 확인',
          fullName: '전체 이름',
          email: '이메일',
        },
      },
      login: {
        title: 'OAuth2에 로그인',
        errors: {
          idRequired: 'ID를 입력하세요.',
          accountNotFound: '일치하는 계정이 존재하지 않습니다.',
          accountLookupFailed: '계정 확인에 실패했습니다. 잠시 후 다시 시도해 주세요.',
          passwordRequired: '암호를 입력하세요.',
          invalidPassword: '암호가 올바르지 않습니다.',
          failed: '로그인에 실패했습니다. 잠시 후 다시 시도해 주세요.',
        },
      },
      register: {
        title: '계정 등록',
        descriptions: {
          id: '사용할 ID를 입력하세요.',
          password: '사용할 비밀번호를 입력하세요.',
          properties: '이름과 이메일을 입력하세요.',
        },
        errors: {
          idRequired: 'ID는 비어있을 수 없습니다.',
          idExists: 'ID가 이미 존재합니다.',
          idCheckFailed: 'ID 중복 확인에 실패했습니다. 잠시 후 다시 시도해 주세요.',
          passwordRequired: '암호는 비어있을 수 없습니다.',
          passwordMismatch: '암호가 일치하지 않습니다.',
          fullNameRequired: '전체 이름을 입력하세요.',
          emailRequired: '이메일을 입력하세요.',
          invalidEmail: '올바르지 않은 이메일입니다.',
          conflict: '이미 사용 중인 ID 또는 이메일입니다.',
          failed: '계정 등록에 실패했습니다. 잠시 후 다시 시도해 주세요.',
        },
      },
      verifyEmail: {
        titles: {
          pending: '이메일 인증 필요',
          verifying: '이메일 인증 중',
          verified: '이메일 인증 완료',
          failed: '이메일 인증 실패',
        },
        descriptions: {
          pending: '메일에 포함된 링크를 클릭하여 이메일 인증을 완료해 주세요.',
          verifying: '인증 링크를 확인하고 있습니다.',
          verified: '이메일 인증이 완료되었습니다. 이제 로그인할 수 있습니다.',
          failed: '인증 링크가 올바르지 않거나 만료되었습니다. 로그인 후 인증 메일을 다시 요청해 주세요.',
          resending: '새로운 인증 메일을 보내고 있습니다.',
          resent: '새로운 인증 메일을 보냈습니다. 받은 편지함을 확인해 주세요.',
        },
        actions: {
          resend: '인증 메일 다시 보내기',
          goToLogin: '로그인으로 이동',
        },
      },
      state: {
        loading: '불러오는 중...',
        unauthorized: '인증되지 않음',
      },
    },
  },
  en: {
    app: {
      common: {
        actions: {
          continue: 'Continue',
          back: 'Back',
          register: 'Register',
        },
        fields: {
          id: 'ID',
          password: 'Password',
          passwordConfirmation: 'Confirm password',
          fullName: 'Full name',
          email: 'Email',
        },
      },
      login: {
        title: 'Sign in to OAuth2',
        errors: {
          idRequired: 'Enter your ID.',
          accountNotFound: 'No matching account was found.',
          accountLookupFailed: 'Could not check the account. Please try again later.',
          passwordRequired: 'Enter your password.',
          invalidPassword: 'The password is incorrect.',
          failed: 'Sign-in failed. Please try again later.',
        },
      },
      register: {
        title: 'Create an account',
        descriptions: {
          id: 'Enter the ID you want to use.',
          password: 'Enter the password you want to use.',
          properties: 'Enter your name and email address.',
        },
        errors: {
          idRequired: 'ID cannot be empty.',
          idExists: 'This ID already exists.',
          idCheckFailed: 'Could not check ID availability. Please try again later.',
          passwordRequired: 'Password cannot be empty.',
          passwordMismatch: 'Passwords do not match.',
          fullNameRequired: 'Enter your full name.',
          emailRequired: 'Enter your email address.',
          invalidEmail: 'Enter a valid email address.',
          conflict: 'This ID or email address is already in use.',
          failed: 'Account registration failed. Please try again later.',
        },
      },
      verifyEmail: {
        titles: {
          pending: 'Email verification required',
          verifying: 'Verifying your email',
          verified: 'Email verified',
          failed: 'Email verification failed',
        },
        descriptions: {
          pending: 'Select the link in the email to complete verification.',
          verifying: 'Checking the verification link.',
          verified: 'Your email has been verified. You can now sign in.',
          failed: 'The verification link is invalid or has expired. Sign in to request a new verification email.',
          resending: 'Sending a new verification email.',
          resent: 'A new verification email has been sent. Check your inbox.',
        },
        actions: {
          resend: 'Resend verification email',
          goToLogin: 'Go to sign in',
        },
      },
      state: {
        loading: 'Loading...',
        unauthorized: 'Unauthorized',
      },
    },
  },
  es: {
    app: {
      common: {
        actions: {
          continue: 'Continuar',
          back: 'Atrás',
          register: 'Registrarse',
        },
        fields: {
          id: 'ID',
          password: 'Contraseña',
          passwordConfirmation: 'Confirmar contraseña',
          fullName: 'Nombre completo',
          email: 'Correo electrónico',
        },
      },
      login: {
        title: 'Iniciar sesión en OAuth2',
        errors: {
          idRequired: 'Introduce tu ID.',
          accountNotFound: 'No se encontró ninguna cuenta coincidente.',
          accountLookupFailed: 'No se pudo comprobar la cuenta. Inténtalo de nuevo más tarde.',
          passwordRequired: 'Introduce tu contraseña.',
          invalidPassword: 'La contraseña no es correcta.',
          failed: 'No se pudo iniciar sesión. Inténtalo de nuevo más tarde.',
        },
      },
      register: {
        title: 'Crear una cuenta',
        descriptions: {
          id: 'Introduce el ID que deseas usar.',
          password: 'Introduce la contraseña que deseas usar.',
          properties: 'Introduce tu nombre y correo electrónico.',
        },
        errors: {
          idRequired: 'El ID no puede estar vacío.',
          idExists: 'Este ID ya existe.',
          idCheckFailed: 'No se pudo comprobar la disponibilidad del ID. Inténtalo de nuevo más tarde.',
          passwordRequired: 'La contraseña no puede estar vacía.',
          passwordMismatch: 'Las contraseñas no coinciden.',
          fullNameRequired: 'Introduce tu nombre completo.',
          emailRequired: 'Introduce tu correo electrónico.',
          invalidEmail: 'Introduce un correo electrónico válido.',
          conflict: 'Este ID o correo electrónico ya está en uso.',
          failed: 'No se pudo registrar la cuenta. Inténtalo de nuevo más tarde.',
        },
      },
      verifyEmail: {
        titles: {
          pending: 'Se requiere verificar el correo',
          verifying: 'Verificando tu correo',
          verified: 'Correo verificado',
          failed: 'Error al verificar el correo',
        },
        descriptions: {
          pending: 'Selecciona el enlace del correo para completar la verificación.',
          verifying: 'Comprobando el enlace de verificación.',
          verified: 'Tu correo se ha verificado. Ya puedes iniciar sesión.',
          failed: 'El enlace de verificación no es válido o ha caducado. Inicia sesión para solicitar un nuevo correo.',
          resending: 'Enviando un nuevo correo de verificación.',
          resent: 'Se ha enviado un nuevo correo de verificación. Revisa tu bandeja de entrada.',
        },
        actions: {
          resend: 'Reenviar correo de verificación',
          goToLogin: 'Ir a iniciar sesión',
        },
      },
      state: {
        loading: 'Cargando...',
        unauthorized: 'No autorizado',
      },
    },
  },
} as const;
