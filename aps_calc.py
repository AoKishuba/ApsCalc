#!/usr/bin/env python

"""Calculate performance of an APS shell.  All dimensions in mm.  Does not
support Graviton Ram.

Classes:
Shell -- a collection of APS modules

Functions:
"""

import math

"""Module Stats:
name -- name as shown ingame
v_mod -- velocity modifier 
ap_mod -- armour pierce modifier
kd_mod -- kinetic damage modifier 
payload_mod -- payload damage modifier.  NOTICE: the Disruptor Conduit has a 
    payload mod of 0.5, but its payload mod stacks, unlike the others, 
    so it must be listed differently in order for the get_chemical_damage 
    method to work.
maxlength -- maximum length, in mm
is_chem -- True if the module carries a chemical payload (such as HE)     
"""

MODULES = {
    'BASE BLEEDER': {
        'name': 'Base Bleeder',
        'v_mod': 1.15,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 100,
        'is_chem': False,
        'can_be_required': True
    },

    'SUPERCAVITATION BASE': {
        'name': 'Supercavitation Base',
        'v_mod': 1,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 0.75,
        'maxlength': 100,
        'is_chem': False,
    },

    'VISIBLE TRACER': {
        'name': 'Tracer',
        'v_mod': 1,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 100,
        'is_chem': False,
    },

    'SOLID BODY': {
        'name': 'Solid Body',
        'v_mod': 1.1,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 500,
        'is_chem': False,
    },

    'SABOT BODY': {
        'name': 'Sabot Body',
        'v_mod': 1.1,
        'ap_mod': 1.4,
        'kd_mod': 0.8,
        'payload_mod': 0.25,
        'maxlength': 500,
        'is_chem': False,
    },

    'CHEM BODY': {
        'name': 'Chemical Body',
        'v_mod': 1,
        'ap_mod': 0.1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 500,
        'is_chem': True,
    },

    'FUSE': {
        'name': 'Fuse',
        'v_mod': 1,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 100,
        'is_chem': False,
    },

    'STABILIZER FIN BODY': {
        'name': 'Fin',
        'v_mod': 0.95,
        'ap_mod': 1,
        'kd_mod': 1,
        'payload_mod': 1,
        'maxlength': 300,
        'is_chem': False,
    }
}

"""Module Stats:
name -- name as shown ingame
v_mod -- velocity modifier 
ap_mod -- armour pierce modifier
kd_mod -- kinetic damage modifier 
payload_mod -- payload damage modifier.  NOTICE: the Disruptor Conduit has a 
    payload mod of 0.5, but its payload mod stacks, unlike the others, 
    so it must be listed differently in order for the get_chemical_damage 
    method to work.
is_chem -- True if the module carries a chemical payload (such as HE)
is_head -- True if the module can only be the frostmost part of the shell
"""

HEADS = {
    'CHEM HEAD': {
        'name': 'Chemical Head',
        'v_mod': 1.3,
        'ap_mod': 0.1,
        'kd_mod': 1,
        'payload_mod': 1,
        'is_chem': True,
    },

    'SQUASH HEAD': {
        'name': 'Squash Head',
        'v_mod': 1.45,
        'ap_mod': 0.1,
        'kd_mod': 0.1,
        'payload_mod': 1,
        'is_chem': True,
    },

    'SHAPED CHARGE HEAD': {
        'name': 'Shaped Charge Head',
        'v_mod': 1.45,
        'ap_mod': 0.1,
        'kd_mod': 0.1,
        'payload_mod': 1,
        'is_chem': True,
    },

    'ARMOR PIERCING HEAD': {
        'name': 'AP Head',
        'v_mod': 1.6,
        'ap_mod': 1.65,
        'kd_mod': 1,
        'payload_mod': 1,
        'is_chem': False,
    },

    'SABOT HEAD': {
        'name': 'Sabot Head',
        'v_mod': 1.6,
        'ap_mod': 2.5,
        'kd_mod': 0.85,
        'payload_mod': 0.25,
        'is_chem': False,
    },

    'HEAVY HEAD': {
        'name': 'Heavy Head',
        'v_mod': 1.45,
        'ap_mod': 1,
        'kd_mod': 1.65,
        'payload_mod': 1,
        'is_chem': False,
    },

    'HOLLOW POINT HEAD': {
        'name': 'Hollow Point',
        'v_mod': 1.45,
        'ap_mod': 1,
        'kd_mod': 1.2,
        'payload_mod': 1,
        'is_chem': False,
    },

    'SKIMMER TIP': {
        'name': 'Skimmer Tip',
        'v_mod': 1.6,
        'ap_mod': 1.4,
        'kd_mod': 1,
        'payload_mod': 1,
        'is_chem': False,
    },

    'DISRUPTOR CONDUIT': {
        'name': 'Disruptor',
        'v_mod': 1.6,
        'ap_mod': 0.1,
        'kd_mod': 1,
        'payload_mod': 0.5,
        'is_chem': True,
    }
}


class Shell:
    """Store information about shell configuration.

    Attributes:
        head -- type of head (AP, HP, disruptor, chemical, &c)
        gauge -- diameter of shell in mm, shown ingame as 'gauge'
        count_gp -- decimal gunpowder casings
        count_rg -- railgun casings
        modules -- list of modules, with 0 being the rearmost non-casing
        total_length -- length in mm of all modules and casings
        proj_length -- length in mm of non-casing modules
        casing_length -- length in mm of casings only
        gp_recoil -- recoil from gunpowder casings
        max_draw -- maximum rail draw the shell can handle
        draw -- amount of rail draw used to fire the shell
        reload_time -- reload time per intake, in seconds
        beltfed_reload_time -- reload time if using beltfed autoloader
        cooldown_time -- barrel cooldown time in seconds
        velocity -- velocity in m/s
        kinetic_damage -- kinetic damage, or thump if using hollow point head
        armor_pierce -- armor pierce
        kinetic_dps -- kinetic (or thump) damage per second
        kinetic_dps_belt -- kinetic (or thump) damage per second from beltfed
        chem_damage -- relative damage of chemical payload, such as HE
        chem_dps -- relative damage per second of chemical payload, such as HE
        chem_dps_belt -- relative chemical damage per second from beltfed

    Methods:
        set_head -- set type of head
        set_gauge -- set gauge in mm
        set_rg -- set number of railgun casings
        set_gp -- set number of gunpowder casings
        add_module -- add a module to the modules list
        get_lengths -- calculate casing, projectile, and total lengths
        get_gp_recoil -- calculate recoil from gunpowder casings
        get_max_draw -- calculate maximum rail draw the shell can handle
        set_draw -- set the amount of draw used to fire the shell
        get_reload_time -- calculate shell reload time in seconds
        get_cooldown_time -- calculate cooldown time in seconds
        get_velocity -- calculate velocity in m/s
        get_kinetic_damage -- calculate kinetic damage
        get_armor_pierce -- calculate armor pierce
        get_kinetic_dps -- calculate kinetic (or thump) damage per second
        get_chem_damage -- calculate relative damage for chemical payloads
        get_chem_dps -- calculate relative damage per second from chem payloads
        show_stats -- return all shell attributes as a dictionary
        """

    def __init__(self):
        self.head = None
        self.gauge = 0
        self.count_gp = 0.00
        self.count_rg = 0
        self.modules = []
        self.total_length = 0
        self.proj_length = 0
        self.casing_length = 0
        self.gp_recoil = 0
        self.max_draw = 0
        self.draw = 0
        self.reload_time = 0.00
        self.beltfed_reload = 0.00
        self.cooldown_time = 0.00
        self.velocity = 0
        self.kinetic_damage = 0
        self.armor_pierce = 0.0
        self.kinetic_dps = 0.0
        self.kinetic_dps_belt = 0.0
        self.chem_damage = 0.0
        self.chem_dps = 0.0
        self.chem_dps_belt = 0.0

    def set_gauge(self, gauge: int) -> None:
        """"Set the shell gauge in mm.

        Arguments:
            gauge -- gauge in mm.  Should be 18 - 500, inclusive.
        """

        self.gauge = round(gauge)

    def set_rg(self, rg: int) -> None:
        """Set the number of railgun casings.

        Arguments:
            rg -- number of railgun casings.  Can only be whole numbers.
        """

        self.count_rg = rg

    def set_gp(self, gp: float) -> None:
        """Set the number of gunpowder casings.

        Arguments:
            gp -- number of gunpowder casings.  Up to two decimal places.
        """

        gp = round(gp, 2)
        self.count_gp = gp

    def add_module(self, mod_name: str) -> None:
        """Add a module from the MODULES dictionary to the shell.

        Since the modules themselves are dictionaries, add_module adds all
        known information about the module to the list.

        Arguments:
            mod_name -- name of module, which must match key from MODULES dict.
        """

        # MODULES stores names in all caps.
        mod_name = mod_name.upper()
        self.modules.append(MODULES[mod_name])

    def set_head(self, head_name: str) -> None:
        """Select the head type from the HEADS dictionary.

        Since the heads themselves are dictionaries, set_head adds all known
        information about the module to the list.

        Arguments:
            head_name -- name of the head, which must match key a from the
            HEADS dict.
        """

        # HEADS stores names in all caps.
        head_name = head_name.upper()
        self.head = HEADS[head_name]

    def get_lengths(self) -> None:
        """Calculate lengths of shell:

        Casing length -- length of gunpowder and railgun casings, if any
        Projectile length -- length of non-casing modules
        Total length -- length of modules and casings

        The length of a module is generally equal to the gauge.  However, some
        modules (fuses, fins, and bases) have maximum length restrictions.
        Additionally, gunpowder casings are not discrete, and can be set up
        to two decimal places.
        """

        proj_length = 0

        casing_length = ((self.count_gp + self.count_rg)
                         * self.gauge)
        casing_length = round(casing_length)
        self.casing_length = casing_length

        for mod in self.modules:
            module_length = min(self.gauge, mod['maxlength'])
            proj_length = proj_length + module_length
        # Heads have no length restrictions
        if self.head is not None:
            proj_length = proj_length + self.gauge
        proj_length = round(proj_length)
        self.proj_length = proj_length

        total_length = round(proj_length + casing_length)
        self.total_length = total_length

    def get_gp_recoil(self) -> None:
        """Calculate recoil from gunpowder casings."""

        recoil = ((((self.gauge ** 3) / (500 ** 3)) ** 0.6)
                  * self.count_gp * 2500)

        self.gp_recoil = recoil

    def get_max_raildraw(self) -> None:
        """Calculate maximum possible rail draw."""

        max_draw = ((((self.gauge ** 3) / (500 ** 3)) ** 0.6)
                    * (self.proj_length / self.gauge + 0.5 * self.count_rg)
                    * 12500)

        max_draw = int(max_draw)
        self.max_draw = max_draw

    def set_draw(self, draw: int) -> None:
        """Set rail draw.

        Arguments:
            draw -- rail draw
        """

        self.draw = draw

    def get_reload_time(self) -> None:
        """Calculate reload time in seconds.

        Also calculate beltfed reload time for shells <= 100 mm gauge."""

        reload = ((((self.gauge ** 3) / (500 ** 3)) ** 0.45)
                  * (2 + self.proj_length / self.gauge
                     + 0.25 * self.casing_length / self.gauge)
                  * 17.5)

        self.reload_time = reload

        # Beltfed reload
        if self.gauge <= 100:
            beltfed_reload = (self.reload_time
                              * ((self.gauge / 1000) ** 0.45)
                              * 0.75)

            self.beltfed_reload = beltfed_reload

    def get_cooldown_time(self) -> None:
        """Calculate barrel cooldown time."""

        cooldown = (3.75
                    * self.reload_time
                    * (self.count_gp ** 0.35)
                    / ((2 + self.proj_length / self.gauge
                        + 0.25 * self.casing_length / self.gauge)
                       * 2))

        self.cooldown_time = cooldown

    def get_velocity(self) -> None:
        """Calculate projectile velocity.

        For the purposes of the velocity calculation, the 'head' - the
        frontmost module - is separated from the rest of the shell body.  A
        weighted average is taken of the velocity modifiers of every non-head
        module on the body, and this is multiplied with the velocity
        modifier of the head.
        """

        vmod_head = self.head['v_mod']

        # Make a list of velocity modifiers of every module in the shell
        vmod_list = []
        for module in self.modules:
            vmod_list.append(module['v_mod'])

        # Take the average of all non-head velocity modifiers
        vmod_body = sum(vmod_list) / len(vmod_list)

        velocity = ((((self.draw + self.gp_recoil)
                      * 85
                      * vmod_body
                      * vmod_head
                      * self.gauge
                      / ((((self.gauge ** 3) / (500 ** 3)) ** 0.6)
                         * self.proj_length)))
                    ** 0.5)

        self.velocity = velocity

    def get_kinetic_damage(self) -> None:
        """Calculate kinetic damage.  For the purposes of the kinetic damage
        calculation, the 'head' - the frontmost module - is separated from
        the rest of the shell body.  A weighted average is taken of the
        kinetic damage modifiers of every non-head module on the body,
        and this is multiplied with the kinetic damage modifier of the head.
        """

        kdmod_head = self.head['kd_mod']

        # Make a list of kinetic damage modifiers of every module in the shell
        kdmod_list = []
        for module in self.modules:
            kdmod_list.append(module['kd_mod'])

        # Take the average of all non-head kinetic damage modifiers
        kdmod_body = sum(kdmod_list) / len(kdmod_list)

        kinetic_damage = ((((self.gauge ** 3) / (500 ** 3)) ** 0.6)
                          * (self.proj_length / self.gauge)
                          * self.velocity
                          * kdmod_body
                          * kdmod_head)

        kinetic_damage = round(kinetic_damage)
        self.kinetic_damage = kinetic_damage

    def get_armor_pierce(self) -> None:
        """Calculate armor pierce.  For the purposes of the ap calculation,
        the 'head' - the frontmost module - is separated from the rest of the
        shell body.  A weighted average is taken of the ap modifiers of
        every non-head module on the body, and this is multiplied with the
        ap modifier of the head.
        """

        apmod_head = self.head['ap_mod']

        # Make a list of armor pierce modifiers of every module in the shell
        apmod_list = []
        for module in self.modules:
            apmod_list.append(module['ap_mod'])

        # Take the average of all non-head armor pierce modifiers
        apmod_body = sum(apmod_list) / len(apmod_list)

        armor_pierce = (self.velocity
                        * apmod_head
                        * apmod_body
                        * 0.0175)

        self.armor_pierce = armor_pierce

    def get_kinetic_dps(self, ac: float) -> None:
        """Calculate kinetic/thump damage per second against given AC.

        Arguments:
            ac -- armor class of target
        """

        kd_effective = self.kinetic_damage * min(1.0, (self.armor_pierce / ac))
        dps = kd_effective / self.reload_time

        # Beltfed only works with 100 mm or smaller gauge
        if self.gauge <= 100:
            dps_belt = kd_effective / self.beltfed_reload
            self.kinetic_dps_belt = dps_belt

        self.kinetic_dps = dps

    def get_chem_damage(self) -> None:
        """Calculate chemical damage modifier.  All chemical warheads -
        HE, frag, FlaK, and EMP - scale the same way, and the numbers given
        by the equation do not necessarily reflect actual performance for
        any type of damage, so it is not necessary to calculate the exact
        rated damage.
        """

        # Count chemical bodies and heads
        chem_count = 0
        for mod in self.modules:
            if mod['is_chem']:
                chem_count = chem_count + 1

        if self.head['is_chem']:
            chem_count = chem_count + 1

        # Find payload mod.  Disruptor stacks, but others don't.
        payloadmod_list = []
        for mod in self.modules:
            payloadmod_list.append(mod['payload_mod'])

        if self.head['name'] == 'Disruptor':
            payloadmod = min(payloadmod_list) * 0.5

        else:
            payloadmod_list.append(self.head['payload_mod'])
            payloadmod = min(payloadmod_list)

        chem_mod = ((((self.gauge ** 3) / (500 ** 3)) ** 0.6)
                    * chem_count)

        chem_mod = chem_mod * payloadmod

        self.chem_damage = chem_mod

    def get_chem_dps(self) -> None:
        """Calculate relative chemical warhead damage per second. Calculated
        damage values for chemical payloads are always imprecise, so simply
        calculating the modifier is sufficient for comparison between
        shells.
        """

        chem_dps = self.chem_damage / self.reload_time

        # Beltfed only works with 100 mm or smaller gauge
        if self.gauge <= 100:
            dps_belt = self.chem_damage / self.beltfed_reload
            self.chem_dps_belt = dps_belt

        self.chem_dps = chem_dps

    def show_stats(self) -> dict:
        """Generate a dictionary containing shell stats."""

        module_names = []

        for mod in self.modules:
            module_names.append(mod['name'])
        module_names.append(self.head['name'])

        stats = {
            'gauge': self.gauge,
            'length': self.total_length,
            'gp_casings': self.count_gp,
            'rg_casings': self.count_rg,
            'modules': module_names,
            'ap': self.armor_pierce,
            'kinetic_damage': self.kinetic_damage,
            'kinetic_dps': self.kinetic_dps,
            'chemical_damage': self.chem_damage,
            'chemical_dps': self.chem_dps,
            'draw': self.draw,
            'recoil': self.draw + self.gp_recoil,
            'reload_time': self.reload_time,
            'RPM': 60 / self.reload_time,
            'velocity': self.velocity
        }

        if self.gauge <= 100:
            stats.update({'kinetic_dps_belt': self.kinetic_dps_belt,
                          'chemical_dps_belt': self.chem_dps_belt,
                          'reload_time_belt': self.beltfed_reload,
                          'RPM_belt': 60 / self.beltfed_reload})

        return stats


if __name__ == '__main__':
    # Create list of required modules
    required_modules = []

    while True:
        for m in MODULES.keys():
            print(m)
        module_name = input('Choose a module from the list above to add to '
                            'the required modules (eg, fuse for a shell with '
                            'a fuse).\nLeave blank when finished.\n')
        module_name = module_name.upper()
        if module_name == '':
            break
        elif module_name in MODULES.keys():
            required_modules.append(module_name)
        else:
            print('\nError: module name not found in dictionary.')

    # Create list of modules to be added
    other_modules = []

    while len(other_modules) < 2:
        for m in MODULES.keys():
            print(m)
        module_name = input('Choose a module from the list above to be added '
                            'during testing.  For example, solid body for '
                            'kinetic shells.  Up to two modules total can be '
                            'chosen, one at a time.\nLeave blank when '
                            'finished.\n')

        module_name = module_name.upper()
        if module_name == '':
            break
        elif module_name in MODULES.keys():
            other_modules.append(module_name)
        else:
            print('\nError: module name not found in dictionary.')

    # Create list of heads to be tried
    head_list = []

    while True:
        for h in HEADS.keys():
            print(h)
        head_name = input('Choose a head from the list above to add to the '
                          'list of heads to be tried.\nLeave blank if '
                          'finished.\n')

        head_name = head_name.upper()
        if head_name == '':
            break
        elif head_name in HEADS.keys():
            head_list.append(head_name)
        else:
            print('\nError: module name not found in dictionary.')

    # Calculate minimum number of modules
    minimum_modules = (len(required_modules)
                       + min(1, len(other_modules))
                       + min(1, len(head_list)))
    max_other_modules = 20 - minimum_modules

    # Get max desired rail draw.
    desired_max_draw = 0
    while True:
        max_draw_input = input('\nEnter max desired rail draw.  Shells will '
                               'be tested at every value up to this '
                               'amount.  Limit 200 000.\n')
        max_draw_input = int(max_draw_input)
        if max_draw_input > 200000:
            print('\nError: number cannot be greater than 200 000\n')
        else:
            desired_max_draw = max_draw_input
            break

    # Get max desired GP casings.
    desired_max_gp = 0
    while True:
        max_gp_input = input('\nEnter max desired gunpowder casings.  Shells '
                             'will be tested at every value up to this '
                             'amount.  Limit ' + str(max_other_modules)
                             + '.\n')
        max_gp_input = int(max_gp_input)
        if max_gp_input > max_other_modules:
            print('\nError: number cannot be greater than'
                  + str(max_other_modules) + '.\n')
        else:
            desired_max_gp = max_gp_input
            break

    # Get max desired RG casings.
    desired_max_rg = 0
    while True:
        max_rg_input = input('\nEnter max desired railgun casings.  Shells '
                             'will be tested at every value up to this '
                             'amount.  Limit ' + str(max_other_modules)
                             + '.\n')
        max_rg_input = int(max_rg_input)
        if max_rg_input > max_other_modules:
            print('\nError: number cannot be greater than'
                  + str(max_other_modules) + '.\n')
        else:
            desired_max_rg = max_rg_input
            break

    # Create list of target AC values, to store top-DPS shells
    top_dps_by_ac = {}

    while True:
        target_ac_input = input('\nEnter target AC value.  \nLeave blank when '
                                'finished.\n')
        if target_ac_input == '':
            break
        else:
            target_ac_input = float(target_ac_input)
            if target_ac_input >= 0.1:
                # Sort shells by loader length
                top_dps_by_ac[target_ac_input] = {1000: {'kinetic_dps': 0.0},
                                                  2000: {'kinetic_dps': 0.0},
                                                  4000: {'kinetic_dps': 0.0},
                                                  6000: {'kinetic_dps': 0.0},
                                                  8000: {'kinetic_dps': 0.0},
                                                  10000: {'kinetic_dps': 0.0}
                                                  }
            else:
                print('\nError: must be greater than 0.1.')

    '''Build shells using every possible combination of factors.  Works by 
    starting at minimum values, then incrementing one at a time in a 
    cascade.
    '''

    print('Testing...')
    shell_count = 0
    shell_count_print = 10000

    for gauge in range(18, 500):
        for head in head_list:
            # Workaround for Python range() not supporting float
            for gp in range(0, max_other_modules * 100, 100):
                gp = gp / 100.00
                max_rg = math.floor(max_other_modules - gp)
                for rg in range(0, max_rg):
                    max_other_1 = max_rg - rg
                    for other_1 in range(0, max_other_1):
                        max_other_2 = max_other_1 - other_1
                        # Set up shell
                        for other_2 in range(0, max_other_2):
                            test_shell = Shell()
                            test_shell.set_gauge(gauge)
                            test_shell.set_head(head)
                            test_shell.set_gp(gp)
                            test_shell.set_rg(rg)
                            # Add modules
                            for mod in required_modules:
                                test_shell.add_module(mod)
                            for mod_1 in range(1, other_1):
                                test_shell.add_module(other_modules[0])
                            for mod_2 in range(1, other_2):
                                test_shell.add_module((other_modules[1]))
                            # Skip test if current draw limit exceeded
                            test_shell.get_lengths()
                            test_shell.get_max_raildraw()
                            shell_max_draw = min(test_shell.max_draw,
                                                 desired_max_draw)
                            for draw in range(0, shell_max_draw):
                                test_shell.set_draw(draw)
                                test_shell.get_gp_recoil()
                                test_shell.get_velocity()
                                test_shell.get_kinetic_damage()
                                test_shell.get_armor_pierce()
                                test_shell.get_reload_time()
                                test_shell.get_chem_damage()
                                if test_shell.chem_damage > 0:
                                    test_shell.get_chem_dps()
                                for ac in top_dps_by_ac.keys():
                                    test_shell.get_kinetic_dps(ac)
                                    shell_stats = test_shell.show_stats()
                                    # Sort DPS by loader length
                                    for loader in top_dps_by_ac[ac].keys():
                                        if test_shell.total_length <= loader:
                                            if test_shell.kinetic_dps > \
                                                    top_dps_by_ac[ac][
                                                        loader]['kinetic_dps']:
                                                top_dps_by_ac[ac][loader] = \
                                                    shell_stats
                                if shell_count >= shell_count_print:
                                    print('Shells tested: ' + str(shell_count))
                                    shell_count_print = (shell_count_print
                                                         + 10000)
                                shell_count = shell_count + 1
    for shell in top_dps_by_ac.items():
        print(shell)
