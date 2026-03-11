import { noDependencies, sameTag, SheriffConfig } from '@softarc/sheriff-core';

export const sheriffConfig: SheriffConfig = {
  version: 1,

  tagging: {
    'libs/chairly/src': ['chairly-lib'],
    'libs/chairly/src/lib': {
      'bookings/<layer>': ['domain:bookings', 'layer:<layer>'],
      'clients/<layer>': ['domain:clients', 'layer:<layer>'],
      'staff/<layer>': ['domain:staff', 'layer:<layer>'],
      'services/<layer>': ['domain:services', 'layer:<layer>'],
      'settings/<layer>': ['domain:settings', 'layer:<layer>'],
      'billing/<layer>': ['domain:billing', 'layer:<layer>'],
      'notifications/<layer>': ['domain:notifications', 'layer:<layer>'],
      'settings/<layer>': ['domain:settings', 'layer:<layer>'],
    },
    'libs/shared/src': ['shared'],
    'libs/shared/src/lib': {
      ui: ['shared', 'layer:ui'],
      'data-access': ['shared', 'layer:data-access'],
      util: ['shared', 'layer:util'],
    },
  },

  depRules: {
    // App (root) can depend on any library barrel or shared
    root: ['chairly-lib', 'shared'],

    // chairly-lib barrel re-exports from domain layers
    'chairly-lib': [
      'domain:billing',
      'domain:bookings',
      'domain:clients',
      'domain:services',
      'domain:settings',
      'domain:staff',
      'shared',
    ],

    // Domain isolation: domains cannot depend on each other
    'domain:*': [sameTag, 'shared'],

    // Layer rules within a domain:
    // feature -> ui, data-access, models, pipes, util, shared
    // ui -> models, pipes, util, shared
    // data-access -> models, util, shared
    // pipes -> util (pipes can use pure utility functions)
    // models -> nothing (within domain)
    // util -> nothing (within domain)
    'layer:feature': [
      'layer:ui',
      'layer:data-access',
      'layer:models',
      'layer:pipes',
      'layer:util',
      'shared',
    ],
    'layer:ui': ['layer:models', 'layer:pipes', 'layer:util', 'shared'],
    'layer:data-access': ['layer:models', 'layer:util', 'shared'],
    'layer:pipes': ['layer:models', 'layer:util'],
    'layer:models': noDependencies,
    'layer:util': noDependencies,

    // Shared can only depend on other shared
    shared: ['shared'],
  },
};
