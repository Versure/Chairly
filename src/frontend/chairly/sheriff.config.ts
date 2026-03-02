import { noDependencies, sameTag, SheriffConfig } from '@softarc/sheriff-core';

export const sheriffConfig: SheriffConfig = {
  version: 1,

  tagging: {
    'libs/chairly/src/lib': {
      'bookings/<layer>': ['domain:bookings', 'layer:<layer>'],
      'clients/<layer>': ['domain:clients', 'layer:<layer>'],
      'staff/<layer>': ['domain:staff', 'layer:<layer>'],
      'services/<layer>': ['domain:services', 'layer:<layer>'],
      'billing/<layer>': ['domain:billing', 'layer:<layer>'],
      'notifications/<layer>': ['domain:notifications', 'layer:<layer>'],
    },
    'libs/shared/src': ['shared'],
    'libs/shared/src/lib': {
      'ui': ['shared', 'layer:ui'],
      'data-access': ['shared', 'layer:data-access'],
      'util': ['shared', 'layer:util'],
    },
  },

  depRules: {
    // Domain isolation: domains cannot depend on each other
    'domain:*': [sameTag, 'shared'],

    // Layer rules within a domain:
    // feature -> ui, data-access, util, shared
    // ui -> util, shared
    // data-access -> util, shared
    // util -> nothing (within domain)
    'layer:feature': ['layer:ui', 'layer:data-access', 'layer:util', 'shared'],
    'layer:ui': ['layer:util', 'shared'],
    'layer:data-access': ['layer:util', 'shared'],
    'layer:util': noDependencies,

    // Shared can only depend on other shared
    'shared': ['shared'],
  },
};
